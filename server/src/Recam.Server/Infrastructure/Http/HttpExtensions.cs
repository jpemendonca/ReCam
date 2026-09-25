using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Recam.Server.Infrastructure.Hosting;

namespace Recam.Server.Infrastructure.Http;

public static class HttpExtensions
{
    public static IServiceCollection AddRecamHttp(this IServiceCollection services, ServerSettings settings)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;

            // Only the proxies the operator named; not even loopback by default.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var proxy in settings.TrustedProxies)
            {
                options.KnownIPNetworks.Add(proxy);
            }
        });
        services.AddProblemDetails();
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        services.Configure<RateLimiterOptions>(options => options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);
        return services;
    }

    /// <summary>
    /// Behind trusted proxies, the client address and scheme come from X-Forwarded-*; everyone
    /// else's headers are left alone. Unhandled exceptions are bugs: they are logged and answered
    /// with a bare 500 problem, without internal details.
    /// </summary>
    public static WebApplication UseRecamHttp(this WebApplication app, ServerSettings settings)
    {
        if (settings.TrustedProxies.Count > 0)
        {
            app.UseForwardedHeaders();
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseRateLimiter();
        return app;
    }
}
