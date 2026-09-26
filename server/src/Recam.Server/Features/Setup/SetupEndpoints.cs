using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Http;
using Recam.Server.Infrastructure.Network;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Realtime;

namespace Recam.Server.Features.Setup;

/// <summary>
/// How a browser becomes the first Monitor and how it leaves (SPECS.md 6). The old /setup page
/// is gone; its address sends people to the browser Monitor.
/// </summary>
public static class SetupEndpoints
{
    private const string RateLimitPolicy = "first-open";

    public static IServiceCollection AddSetup(this IServiceCollection services)
    {
        services.AddSingleton<FirstOpen>();
        services.AddHostedService<FirstOpenWorker>();
        services.AddRateLimiter(options => options.AddPolicy(RateLimitPolicy, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })));
        return services;
    }

    public static IEndpointRouteBuilder MapSetupEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/setup", () => TypedResults.Redirect("/"));
        endpoints.MapGet("/api/web/first-open", GetStatusAsync);
        endpoints.MapPost("/api/web/first-open", OpenAsync).RequireRateLimiting(RateLimitPolicy);
        endpoints.MapPost("/api/web/sign-out", SignOutAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<Ok<FirstOpenStatusResponse>> GetStatusAsync(FirstOpen firstOpen, CancellationToken cancellationToken) =>
        TypedResults.Ok(new FirstOpenStatusResponse(await firstOpen.IsOpenAsync(cancellationToken)));

    private static async Task<IResult> OpenAsync(
        FirstOpenRequest request,
        HttpContext context,
        FirstOpen firstOpen,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!IsDirectLocalRequest(context))
        {
            return SetupErrors.NotLocal.ToHttpResult();
        }

        var headers = context.Request.GetTypedHeaders();
        var name = BrowserDeviceName.From(context.Request.Headers.UserAgent, headers.AcceptLanguage);
        var opened = await firstOpen.OpenAsync(request.Code, name, cancellationToken);
        if (opened.IsFailure)
        {
            return opened.Error.ToHttpResult();
        }

        DeviceCookie.Append(context.Response, opened.Value.Credential, request.Remember, timeProvider.GetUtcNow());
        return TypedResults.NoContent();
    }

    /// <summary>Leaving revokes the browser on the server, not only its cookie.</summary>
    private static async Task<NoContent> SignOutAsync(
        ClaimsPrincipal user,
        HttpContext context,
        IDbContextFactory<RecamDbContext> databaseFactory,
        TimeProvider timeProvider,
        IDeviceRemovals removals,
        CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var deviceId = user.GetDeviceId();
        var device = await database.Devices.SingleAsync(candidate => candidate.Id == deviceId, cancellationToken);
        device.Revoke(timeProvider.GetUtcNow());
        await database.SaveChangesAsync(cancellationToken);
        await removals.DeviceRemovedAsync(device.Id, device.Role == DeviceRole.Camera);
        DeviceCookie.Delete(context.Response);
        return TypedResults.NoContent();
    }

    // The code only works from the local network. Behind a trusted proxy the forwarded client
    // address is already in place; forwarded headers from anyone else are refused.
    private static bool IsDirectLocalRequest(HttpContext context) =>
        !context.Request.Headers.ContainsKey("X-Forwarded-For")
        && context.Connection.RemoteIpAddress is { } remote
        && remote.IsPrivateOrLoopback();
}
