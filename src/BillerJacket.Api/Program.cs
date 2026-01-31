using BillerJacket.Api.Infrastructure;
using BillerJacket.Api.Infrastructure.Auth;
using BillerJacket.Application.Common;
using BillerJacket.Infrastructure.Data;
using BillerJacket.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
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

builder.Services.AddScoped<ITenantProvider, CurrentTenantProvider>();

builder.Services.AddDbContext<ArDbContext>((sp, options) =>
    options.UseSqlServer(connectionString));

// EF Core - Identity context (needed for seed data migrations)
builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity core (needed for seed data UserManager -- no UI needed in API)
builder.Services.AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppIdentityDbContext>();

// API key authentication
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>("ApiKey", null);

builder.Services.AddAuthorization();

// Controllers
builder.Services.AddControllers();

// Service Bus publisher (with NullBusPublisher fallback)
var sbConnectionString = builder.Configuration.GetConnectionString("ServiceBus");
if (!string.IsNullOrWhiteSpace(sbConnectionString))
{
    builder.Services.AddSingleton(_ => new Azure.Messaging.ServiceBus.ServiceBusClient(sbConnectionString));
    builder.Services.AddScoped<BillerJacket.Api.Infrastructure.Messaging.IBusPublisher,
        BillerJacket.Api.Infrastructure.Messaging.BusPublisher>();
}
else
{
    builder.Services.AddScoped<BillerJacket.Api.Infrastructure.Messaging.IBusPublisher,
        BillerJacket.Api.Infrastructure.Messaging.NullBusPublisher>();
}

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

Current.Initialize(app.Services.GetRequiredService<IHttpContextAccessor>());

// Seed data in development
if (app.Environment.IsDevelopment())
{
    await SeedData.EnsureSeedDataAsync(app.Services);
}

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "BillerJacket.Api" }));

app.Run();
