using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Recam.Server.Infrastructure.Observability;

public static class ObservabilityExtensions
{
    /// <summary>Standard OpenTelemetry variable; the exporter reads it on its own.</summary>
    public const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    private const string ServiceName = "recam-server";

    /// <summary>
    /// Traces of incoming requests and outgoing calls (the WHIP/WHEP proxy to MediaMTX), plus the
    /// metrics of ASP.NET Core, HttpClient and <see cref="RecamMetrics"/>. Nothing leaves the
    /// server unless the operator points <see cref="OtlpEndpointKey"/> at a collector.
    /// </summary>
    public static IServiceCollection AddRecamObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<RecamMetrics>();
        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(RecamMetrics.MeterName));
        if (!string.IsNullOrWhiteSpace(configuration[OtlpEndpointKey]))
        {
            telemetry.UseOtlpExporter();
        }

        return services;
    }

    /// <summary>Creates the gauges at startup, so they exist before the first collection.</summary>
    public static WebApplication StartRecamMetrics(this WebApplication app)
    {
        app.Services.GetRequiredService<RecamMetrics>();
        return app;
    }
}
