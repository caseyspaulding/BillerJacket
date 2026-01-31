using BillerJacket.Application.Common;
using BillerJacket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not configured.");

builder.Services.AddScoped<ITenantProvider, CurrentTenantProvider>();

builder.Services.AddDbContext<ArDbContext>((sp, options) =>
    options.UseSqlServer(connectionString));

// Service Bus consumers (only if connection string is configured)
var sbConnectionString = builder.Configuration.GetConnectionString("ServiceBus");
if (!string.IsNullOrWhiteSpace(sbConnectionString))
{
    builder.Services.AddSingleton(_ => new Azure.Messaging.ServiceBus.ServiceBusClient(sbConnectionString));
    // Register hosted services for each queue processor here
}

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "BillerJacket.Worker" }));

app.Run();
