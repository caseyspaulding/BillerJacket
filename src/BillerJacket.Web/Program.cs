using BillerJacket.Application.Common;
using BillerJacket.Infrastructure.Data;
using BillerJacket.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
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

builder.Services.AddDbContext<ArDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDbContext<AppIdentityDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity
builder.Services.AddDefaultIdentity<AppUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppIdentityDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireClaim("role", "Admin"));
    options.AddPolicy("SuperAdmin", p => p.RequireClaim("role", "SuperAdmin"));
});

// Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AuthorizeFolder("/Admin", "SuperAdmin");
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

Current.Initialize(app.Services.GetRequiredService<IHttpContextAccessor>());

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "BillerJacket.Web" }));

app.Run();
