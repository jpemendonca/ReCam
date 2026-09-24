using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.RateLimiting;

namespace Recam.Server.Infrastructure.Http;

public static class HttpExtensions
{
    public static IServiceCollection AddRecamHttp(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        services.Configure<RateLimiterOptions>(options => options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);
        return services;
    }

    /// <summary>
    /// Unhandled exceptions are bugs: they are logged and answered with a bare 500 problem,
    /// without internal details.
    /// </summary>
    public static WebApplication UseRecamHttp(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseRateLimiter();
        return app;
    }
}
