using BillerJacket.Application.Common;
using BillerJacket.Infrastructure.Data;
using BillerJacket.Infrastructure.Messaging;
using BillerJacket.Worker.Infrastructure;
using BillerJacket.Worker.Processors;
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

builder.Services.AddScoped<MessageScopeTenantProvider>();
builder.Services.AddScoped<ITenantProvider>(sp => sp.GetRequiredService<MessageScopeTenantProvider>());

builder.Services.AddDbContext<ArDbContext>((sp, options) =>
    options.UseSqlServer(connectionString));

// Service Bus consumers (only if connection string is configured)
var sbConnectionString = builder.Configuration.GetConnectionString("ServiceBus");
if (!string.IsNullOrWhiteSpace(sbConnectionString))
{
    builder.Services.AddSingleton(_ => new Azure.Messaging.ServiceBus.ServiceBusClient(sbConnectionString));
    builder.Services.AddScoped<IBusPublisher, BusPublisher>();

    builder.Services.AddHostedService<EmailProcessorHostedService>();
    builder.Services.AddHostedService<DunningProcessorHostedService>();
    builder.Services.AddHostedService<PaymentProcessorHostedService>();
    builder.Services.AddHostedService<WebhookProcessorHostedService>();
}
else
{
    builder.Services.AddScoped<IBusPublisher, NullBusPublisher>();
}

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "BillerJacket.Worker" }));

app.Run();
