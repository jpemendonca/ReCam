namespace Recam.Server.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => TypedResults.Text("ok"));
        return endpoints;
    }
}
