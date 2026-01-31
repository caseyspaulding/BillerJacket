namespace BillerJacket.Web.Middleware;

public class SetupRedirectMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly string[] SkipPrefixes =
        ["/setup", "/logout", "/css/", "/js/", "/favicon", "/_framework"];

    public SetupRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var setupComplete = context.User.FindFirst("setup_complete")?.Value;
            if (setupComplete == "false")
            {
                var path = context.Request.Path.Value ?? string.Empty;
                var skip = SkipPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

                if (!skip)
                {
                    context.Response.Redirect("/setup");
                    return;
                }
            }
        }

        await _next(context);
    }
}

public static class SetupRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseSetupRedirect(this IApplicationBuilder builder)
        => builder.UseMiddleware<SetupRedirectMiddleware>();
}
