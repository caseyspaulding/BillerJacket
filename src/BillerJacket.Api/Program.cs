using BillerJacket.Application.Common;
using BillerJacket.Infrastructure.Data;
using BillerJacket.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// EF Core - AR context (write side)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not configured.");

builder.Services.AddDbContext<ArDbContext>(options =>
    options.UseSqlServer(connectionString));

// EF Core - Identity context
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(connectionString));

// Controllers
builder.Services.AddControllers();

// Service Bus publisher (only if connection string is configured)
var sbConnectionString = builder.Configuration.GetConnectionString("ServiceBus");
if (!string.IsNullOrWhiteSpace(sbConnectionString))
{
    builder.Services.AddSingleton(_ => new Azure.Messaging.ServiceBus.ServiceBusClient(sbConnectionString));
    builder.Services.AddScoped<BillerJacket.Api.Infrastructure.Messaging.IBusPublisher,
        BillerJacket.Api.Infrastructure.Messaging.BusPublisher>();
}

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

Current.Initialize(app.Services.GetRequiredService<IHttpContextAccessor>());

app.UseSerilogRequestLogging();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "BillerJacket.Api" }));

app.Run();
