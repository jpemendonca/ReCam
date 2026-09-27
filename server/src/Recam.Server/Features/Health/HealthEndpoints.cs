using Recam.Server.Infrastructure.Hosting;

namespace Recam.Server.Features.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Open, like before: phones probe it before pairing. The version helps tell builds apart.
        endpoints.MapGet("/health", () => TypedResults.Ok(new HealthResponse("ok", ServerVersion.Current)));
        return endpoints;
    }
}
