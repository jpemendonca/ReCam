using Microsoft.AspNetCore.Http.HttpResults;
using Recam.Server.Infrastructure.Network;

namespace Recam.Server.Features.Setup;

public static class SetupEndpoints
{
    public static IServiceCollection AddSetup(this IServiceCollection services)
    {
        services.AddSingleton<OwnerSetup>();
        services.AddHostedService<OwnerSetupWorker>();
        return services;
    }

    public static IEndpointRouteBuilder MapSetupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/setup", GetSetupPageAsync);
        return endpoints;
    }

    private static async Task<Results<ContentHttpResult, ProblemHttpResult>> GetSetupPageAsync(
        HttpContext context, OwnerSetup ownerSetup, CancellationToken cancellationToken)
    {
        if (!IsDirectLocalRequest(context))
        {
            return TypedResults.Problem(
                title: "The setup page is only available from the local network. Use the QR code in the server log.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var status = await ownerSetup.EnsureTokenAsync(cancellationToken);
        context.Response.Headers.CacheControl = "no-store";
        return TypedResults.Content(SetupPage.Render(status), "text/html; charset=utf-8");
    }

    // The page hands out the owner token. Behind a proxy the direct peer is the proxy itself,
    // so forwarded requests are refused instead of trusted.
    private static bool IsDirectLocalRequest(HttpContext context) =>
        !context.Request.Headers.ContainsKey("X-Forwarded-For")
        && context.Connection.RemoteIpAddress is { } remote
        && remote.IsPrivateOrLoopback();
}
