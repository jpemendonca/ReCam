namespace Recam.Server.Features.Web;

/// <summary>
/// Serves the Monitor that runs in the browser (Recam.Web, Blazor WebAssembly) at the root. The
/// browser is one more client of the same API, hub and WHEP as the app (SPECS.md 2.5).
/// </summary>
public static class WebEndpoints
{
    // Nothing loads from outside the server, and no inline script runs. WebAssembly needs
    // 'wasm-unsafe-eval'.
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; style-src 'self'; img-src 'self' data:; " +
        "media-src 'self' blob:; connect-src 'self'; object-src 'none'; base-uri 'self'; " +
        "form-action 'self'; frame-ancestors 'none'";

    // Paths that belong to the server, never to the browser app: an unknown one there is a 404,
    // not the app's page.
    private const string ClientRoutes = "{*path:regex(^(?!(api|hubs|whip|whep|setup|health)(/|$)).*$)}";

    /// <summary>Security headers on everything the browser app is served from.</summary>
    public static WebApplication UseWebSecurityHeaders(this WebApplication app)
    {
        app.Use((context, next) =>
        {
            // The old /setup page carries its own inline style until bullet 6.2 removes it.
            if (!context.Request.Path.StartsWithSegments("/setup"))
            {
                var headers = context.Response.Headers;
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
                headers.XContentTypeOptions = "nosniff";
                headers["Referrer-Policy"] = "no-referrer";
            }

            return next(context);
        });
        return app;
    }

    public static IEndpointRouteBuilder MapWebEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapStaticAssets();
        endpoints.MapFallbackToFile("/", "index.html");
        endpoints.MapFallbackToFile(ClientRoutes, "index.html");
        return endpoints;
    }
}
