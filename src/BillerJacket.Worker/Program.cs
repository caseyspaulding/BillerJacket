using BillerJacket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Serilog
builder.Services.AddSerilog((_, cfg) => cfg
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console());

// EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection not configured.");

builder.Services.AddDbContext<ArDbContext>(options =>
    options.UseSqlServer(connectionString));

// Service Bus consumers (only if connection string is configured)
var sbConnectionString = builder.Configuration.GetConnectionString("ServiceBus");
if (!string.IsNullOrWhiteSpace(sbConnectionString))
{
    builder.Services.AddSingleton(_ => new Azure.Messaging.ServiceBus.ServiceBusClient(sbConnectionString));
    // Register hosted services for each queue processor here
}

var host = builder.Build();
host.Run();
