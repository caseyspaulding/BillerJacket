using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using System.Data;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BillerJacket Database MCP Server",
        Version = "v1",
        Description = "MCP endpoints for LLM database operations"
    });
});

// CORS for LLM access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLLM", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Database connection factory - raw ADO.NET only
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowLLM");

// Root endpoint
app.MapGet("/", () => new
{
    service = "BillerJacket MCP Server",
    status = "running",
    version = "1.0",
    endpoints = new[]
    {
        "/mcp/query",
        "/mcp/execute",
        "/mcp/validate",
        "/mcp/optimize",
        "/mcp/health",
        "/mcp/context",
        "/mcp/schema/{tableName}"
    }
})
.WithName("Root")
.WithSummary("Service information");

// MCP Endpoints
var mcp = app.MapGroup("/mcp")
    .WithTags("MCP");

// 1. Query endpoint - SELECT queries
mcp.MapPost("/query", async (QueryRequest request, IDbConnectionFactory dbFactory) =>
{
    try
    {
        var trimmedSql = request.Sql.TrimStart();
        if (!trimmedSql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !trimmedSql.StartsWith("EXEC", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "Only SELECT queries and EXEC statements allowed on this endpoint" });
        }

        using var db = dbFactory.CreateConnection();
        await db.OpenAsync();
        using var cmd = new SqlCommand(request.Sql, db);

        if (request.Parameters != null)
        {
            var paramDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(request.Parameters));

            if (paramDict != null)
            {
                foreach (var param in paramDict)
                {
                    cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
            }
        }

        if (trimmedSql.StartsWith("EXEC", StringComparison.OrdinalIgnoreCase))
        {
            cmd.CommandType = CommandType.Text;
        }

        using var reader = await cmd.ExecuteReaderAsync();
        var results = new List<Dictionary<string, object>>();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
            }
            results.Add(row);

            if (results.Count >= (request.Limit ?? 100))
                break;
        }

        return Results.Ok(new
        {
            success = true,
            sql = request.Sql,
            rowCount = results.Count,
            data = results,
            executedAt = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            success = false,
            error = ex.Message,
            sql = request.Sql,
            hint = Helpers.GenerateSqlHint(ex)
        });
    }
})
.WithName("ExecuteQuery")
.WithSummary("Execute a SELECT query with raw ADO.NET");

// 2. Execute endpoint - DDL/DML with safety checks
mcp.MapPost("/execute", async (ExecuteRequest request, IDbConnectionFactory dbFactory, ILogger<Program> logger) =>
{
    logger.LogInformation("MCP Execute: {sql}", request.Sql);

    if (Helpers.IsDangerous(request.Sql) && !request.Force)
    {
        return Results.BadRequest(new
        {
            error = "Potentially dangerous operation detected",
            operations = Helpers.DetectOperations(request.Sql),
            hint = "Add 'force: true' to execute, or use 'dryRun: true' to preview"
        });
    }

    if (request.DryRun)
    {
        return Results.Ok(new
        {
            dryRun = true,
            wouldExecute = request.Sql,
            operations = Helpers.DetectOperations(request.Sql),
            message = "Dry run - no changes made"
        });
    }

    try
    {
        using var db = dbFactory.CreateConnection();
        await db.OpenAsync();

        if (request.Sql.TrimStart().StartsWith("EXEC", StringComparison.OrdinalIgnoreCase))
        {
            using var cmd = new SqlCommand(request.Sql, db);
            cmd.CommandType = CommandType.Text;

            var result = await cmd.ExecuteScalarAsync();

            return Results.Ok(new
            {
                success = true,
                result = result,
                sql = request.Sql,
                executedAt = DateTime.UtcNow
            });
        }
        else
        {
            using var cmd = new SqlCommand(request.Sql, db);
            var affected = await cmd.ExecuteNonQueryAsync();

            return Results.Ok(new
            {
                success = true,
                rowsAffected = affected,
                sql = request.Sql,
                executedAt = DateTime.UtcNow
            });
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "MCP Execute failed");
        return Results.Ok(new
        {
            success = false,
            error = ex.Message,
            sql = request.Sql,
            hint = Helpers.GenerateSqlHint(ex)
        });
    }
})
.WithName("ExecuteCommand")
.WithSummary("Execute DDL/DML commands with safety checks");

// 3. Validate endpoint - Check SQL syntax without executing
mcp.MapPost("/validate", async (ValidateRequest request, IDbConnectionFactory dbFactory) =>
{
    try
    {
        using var db = dbFactory.CreateConnection();
        await db.OpenAsync();

        var validationSql = $"SET PARSEONLY ON; {request.Sql}; SET PARSEONLY OFF;";
        using var cmd = new SqlCommand(validationSql, db);
        await cmd.ExecuteNonQueryAsync();

        return Results.Ok(new
        {
            valid = true,
            message = "SQL syntax is valid",
            operations = Helpers.DetectOperations(request.Sql)
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            valid = false,
            error = ex.Message,
            hint = Helpers.GenerateSqlHint(ex)
        });
    }
})
.WithName("ValidateSQL")
.WithSummary("Validate SQL syntax without executing");

// 4. Optimize endpoint - Analyze and optimize queries
mcp.MapPost("/optimize", async (OptimizeRequest request, IDbConnectionFactory dbFactory) =>
{
    try
    {
        using var db = dbFactory.CreateConnection();
        await db.OpenAsync();

        var optimizations = Helpers.GenerateOptimizations(request.Sql);

        return Results.Ok(new
        {
            originalSql = request.Sql,
            recommendations = optimizations
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
})
.WithName("OptimizeQuery")
.WithSummary("Analyze and optimize SQL query");

// 5. Health check endpoint
mcp.MapGet("/health", async (IDbConnectionFactory dbFactory) =>
{
    try
    {
        using var db = dbFactory.CreateConnection();
        await db.OpenAsync();
        using var cmd = new SqlCommand("SELECT 1", db);
        await cmd.ExecuteScalarAsync();

        return Results.Ok(new
        {
            status = "healthy",
            database = db.Database,
            server = db.DataSource,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            status = "unhealthy",
            error = ex.Message
        });
    }
})
.WithName("HealthCheck")
.WithSummary("Check database connection health");

// 6. Context endpoint - List tables with row counts
mcp.MapGet("/context", async (IDbConnectionFactory dbFactory) =>
{
    try
    {
        using var db = dbFactory.CreateConnection();
        await db.OpenAsync();

        var tables = new List<object>();
        using var tablesCmd = new SqlCommand(@"
            SELECT t.TABLE_NAME,
                   (SELECT SUM(p.rows) FROM sys.partitions p
                    JOIN sys.tables st ON p.object_id = st.object_id
                    WHERE st.name = t.TABLE_NAME AND p.index_id < 2) as [RowCount]
            FROM INFORMATION_SCHEMA.TABLES t
            WHERE t.TABLE_TYPE = 'BASE TABLE'
            ORDER BY t.TABLE_NAME", db);

        using var reader = await tablesCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(new
            {
                name = reader.GetString(0),
                rowCount = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1))
            });
        }

        return Results.Ok(new
        {
            database = db.Database,
            tableCount = tables.Count,
            tables = tables,
            hint = "Use db_schema with a table name to get column details"
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            success = false,
            error = ex.Message
        });
    }
})
.WithName("GetContext")
.WithSummary("Get database context - tables and row counts");

// 7. Schema endpoint - Get columns for a specific table
mcp.MapGet("/schema/{tableName}", async (string tableName, IDbConnectionFactory dbFactory) =>
{
    try
    {
        using var db = dbFactory.CreateConnection();
        await db.OpenAsync();

        using var checkCmd = new SqlCommand(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName", db);
        checkCmd.Parameters.AddWithValue("@tableName", tableName);
        var existsResult = await checkCmd.ExecuteScalarAsync();
        var exists = existsResult != null && (int)existsResult > 0;

        if (!exists)
        {
            return Results.NotFound(new { error = $"Table '{tableName}' not found" });
        }

        var columns = new List<object>();
        using var colCmd = new SqlCommand(@"
            SELECT
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.CHARACTER_MAXIMUM_LENGTH,
                c.IS_NULLABLE,
                c.COLUMN_DEFAULT,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IsPrimaryKey
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    AND tc.TABLE_NAME = @tableName
            ) pk ON c.COLUMN_NAME = pk.COLUMN_NAME
            WHERE c.TABLE_NAME = @tableName
            ORDER BY c.ORDINAL_POSITION", db);
        colCmd.Parameters.AddWithValue("@tableName", tableName);

        using var reader = await colCmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(new
            {
                name = reader.GetString(0),
                type = reader.GetString(1),
                maxLength = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2),
                nullable = reader.GetString(3) == "YES",
                defaultValue = reader.IsDBNull(4) ? null : reader.GetString(4),
                isPrimaryKey = reader.GetInt32(5) == 1
            });
        }

        return Results.Ok(new
        {
            table = tableName,
            columnCount = columns.Count,
            columns = columns,
            hint = "Use this schema info to write Entity classes with correct property types"
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            success = false,
            error = ex.Message
        });
    }
})
.WithName("GetSchema")
.WithSummary("Get column details for a specific table");

app.Run();

// DTOs
record QueryRequest(string Sql, object? Parameters = null, int? Limit = 100);
record ExecuteRequest(string Sql, bool DryRun = false, bool Force = false);
record OptimizeRequest(string Sql);
record ValidateRequest(string Sql);

// Helper methods
static class Helpers
{
    public static string GenerateSqlHint(Exception ex)
    {
        if (ex.Message.Contains("Invalid object name"))
            return "Table or view doesn't exist";
        if (ex.Message.Contains("Invalid column name"))
            return "Column doesn't exist";
        if (ex.Message.Contains("Incorrect syntax"))
            return "SQL syntax error - check your query";
        if (ex.Message.Contains("Cannot insert duplicate key"))
            return "Duplicate key violation - check unique constraints";
        return "Check SQL syntax and database permissions";
    }

    public static bool IsDangerous(string sql)
    {
        var upper = sql.ToUpper();
        return upper.Contains("DROP TABLE") ||
               upper.Contains("TRUNCATE") ||
               upper.Contains("DROP DATABASE") ||
               (upper.Contains("DELETE") && !upper.Contains("WHERE")) ||
               (upper.Contains("UPDATE") && !upper.Contains("WHERE"));
    }

    public static List<string> DetectOperations(string sql)
    {
        var operations = new List<string>();
        var upper = sql.ToUpper();

        if (upper.Contains("CREATE TABLE")) operations.Add("CREATE TABLE");
        if (upper.Contains("ALTER TABLE")) operations.Add("ALTER TABLE");
        if (upper.Contains("DROP")) operations.Add("DROP");
        if (upper.Contains("INSERT")) operations.Add("INSERT");
        if (upper.Contains("UPDATE")) operations.Add("UPDATE");
        if (upper.Contains("DELETE")) operations.Add("DELETE");
        if (upper.Contains("CREATE PROCEDURE")) operations.Add("CREATE PROCEDURE");
        if (upper.Contains("ALTER PROCEDURE")) operations.Add("ALTER PROCEDURE");
        if (upper.Contains("EXEC")) operations.Add("EXECUTE PROCEDURE");

        return operations;
    }

    public static List<string> GenerateOptimizations(string sql)
    {
        var optimizations = new List<string>();
        var upperSql = sql.ToUpper();

        if (upperSql.Contains("SELECT *"))
            optimizations.Add("Avoid SELECT * - specify only needed columns");

        if (!upperSql.Contains("WHERE") && (upperSql.Contains("UPDATE") || upperSql.Contains("DELETE")))
            optimizations.Add("UPDATE/DELETE without WHERE clause affects all rows");

        if (upperSql.Contains("LIKE '%"))
            optimizations.Add("LIKE with leading % prevents index usage");

        if (upperSql.Contains("UPPER(") || upperSql.Contains("LOWER(") || upperSql.Contains("DATEPART("))
            optimizations.Add("Functions on columns prevent index usage - consider computed columns");

        if (upperSql.Contains(" OR "))
            optimizations.Add("OR conditions may prevent index usage - consider UNION");

        if (upperSql.Contains("NOT IN"))
            optimizations.Add("NOT IN can be slow - consider NOT EXISTS or LEFT JOIN");

        return optimizations;
    }
}

// Connection factory
public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new InvalidOperationException("Database connection string not found. Configure it in appsettings.json.");
        }
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
