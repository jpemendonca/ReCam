using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Network;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Presence;
using Recam.Server.Infrastructure.Recordings;

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

    /// <summary>
    /// The server's only page: the first phone's QR code while nobody owns the server, then a
    /// read-only panel of the paired devices.
    /// </summary>
    private static async Task<Results<ContentHttpResult, ProblemHttpResult>> GetSetupPageAsync(
        HttpContext context,
        OwnerSetup ownerSetup,
        IDbContextFactory<RecamDbContext> databaseFactory,
        DevicePresence presence,
        RecordingStore recordings,
        CancellationToken cancellationToken)
    {
        if (!IsDirectLocalRequest(context))
        {
            return TypedResults.Problem(
                title: "The setup page is only available from the local network. Use the QR code in the server log.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var texts = SetupTexts.For(context.Request.GetTypedHeaders().AcceptLanguage);
        var status = await ownerSetup.EnsureTokenAsync(cancellationToken);
        var page = status switch
        {
            OwnerSetupStatus.Pending pending => SetupPage.RenderPending(pending, texts),
            OwnerSetupStatus.Configured => SetupPage.RenderPanel(
                await LoadPanelAsync(databaseFactory, presence, cancellationToken),
                await LoadRecordingUsageAsync(databaseFactory, recordings, cancellationToken),
                texts),
            _ => throw new InvalidOperationException($"Unknown setup status {status.GetType().Name}."),
        };
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Vary = "Accept-Language";
        return TypedResults.Content(page, "text/html; charset=utf-8");
    }

    private static async Task<RecordingUsage> LoadRecordingUsageAsync(
        IDbContextFactory<RecamDbContext> databaseFactory, RecordingStore recordings, CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var quota = await RecordingQuotas.LoadAsync(database, cancellationToken);
        return new RecordingUsage(recordings.ListSegments().Sum(segment => segment.Bytes), quota.Bytes);
    }

    private static async Task<List<PanelDevice>> LoadPanelAsync(
        IDbContextFactory<RecamDbContext> databaseFactory, DevicePresence presence, CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var devices = await database.Devices.AsNoTracking()
            .Where(device => device.RevokedAt == null)
            .ToListAsync(cancellationToken);
        return devices
            .OrderBy(device => device.Role == DeviceRole.Camera ? 0 : 1)
            .ThenBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(device => PanelDevice.From(device, presence))
            .ToList();
    }

    // The page hands out the owner token. Behind a proxy the direct peer is the proxy itself,
    // so forwarded requests are refused instead of trusted.
    private static bool IsDirectLocalRequest(HttpContext context) =>
        !context.Request.Headers.ContainsKey("X-Forwarded-For")
        && context.Connection.RemoteIpAddress is { } remote
        && remote.IsPrivateOrLoopback();
}
