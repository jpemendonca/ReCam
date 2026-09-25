using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Auth;
using Recam.Server.Infrastructure.Http;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Presence;
using Recam.Server.Infrastructure.Recordings;

namespace Recam.Server.Features.Recordings;

public static class RecordingEndpoints
{
    public static IServiceCollection AddRecordings(this IServiceCollection services)
    {
        services.AddSingleton<RecordingStore>();
        services.AddSingleton<RecordingCleanupWorker>();
        services.AddHostedService(provider => provider.GetRequiredService<RecordingCleanupWorker>());
        return services;
    }

    public static IEndpointRouteBuilder MapRecordingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var quota = endpoints.MapGroup("/api/recordings/quota").RequireAuthorization(AuthExtensions.ViewerOrOwner);
        quota.MapGet(string.Empty, GetQuotaAsync);
        quota.MapPut(string.Empty, SetQuotaAsync);

        endpoints.MapGet("/api/cameras/{cameraId:guid}/recordings", ListRecordingsAsync)
            .RequireAuthorization(AuthExtensions.ViewerOrOwner);
        endpoints.MapGet("/api/cameras/{cameraId:guid}/recording-days", ListRecordingDaysAsync)
            .RequireAuthorization(AuthExtensions.ViewerOrOwner);
        endpoints.MapGet("/api/recordings/{cameraId:guid}/{segment}", ServeSegment)
            .RequireAuthorization(AuthExtensions.ViewerOrOwner);
        return endpoints;
    }

    /// <summary>The stretches of one UTC day (<c>?day=AAAA-MM-DD</c>).</summary>
    private static async Task<IResult> ListRecordingsAsync(
        Guid cameraId,
        string? day,
        IDbContextFactory<RecamDbContext> databaseFactory,
        RecordingStore store,
        DevicePresence presence,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(day, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return RecordingErrors.InvalidDay.ToHttpResult();
        }

        var segments = await CameraSegmentsAsync(cameraId, databaseFactory, store, presence, cancellationToken);
        if (segments is null)
        {
            return MediaErrors.CameraNotFound.ToHttpResult();
        }

        var pieces = RecordingTimeline.Build(segments.Value.Segments, date, segments.Value.NewestInProgress);
        return TypedResults.Ok(pieces
            .Select(piece => new RecordingPieceResponse(
                piece.StartsAt,
                piece.EndsAt,
                piece.Segments
                    .Select(span => new RecordingSegmentResponse(span.StartsAt, span.EndsAt, SegmentUrl(cameraId, span.FileName)))
                    .ToList()))
            .ToList());
    }

    /// <summary>The UTC days with recordings, newest first.</summary>
    private static async Task<IResult> ListRecordingDaysAsync(
        Guid cameraId,
        IDbContextFactory<RecamDbContext> databaseFactory,
        RecordingStore store,
        DevicePresence presence,
        CancellationToken cancellationToken)
    {
        var segments = await CameraSegmentsAsync(cameraId, databaseFactory, store, presence, cancellationToken);
        if (segments is null)
        {
            return MediaErrors.CameraNotFound.ToHttpResult();
        }

        return TypedResults.Ok(RecordingTimeline.Days(segments.Value.Segments, segments.Value.NewestInProgress)
            .Select(date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .ToList());
    }

    /// <summary>
    /// Serves one segment file with Range support, so a player can seek. Only a name in
    /// MediaMTX's format reaches the disk; anything else is simply not found.
    /// </summary>
    private static IResult ServeSegment(Guid cameraId, string segment, RecordingStore store)
    {
        var path = store.PathOf(cameraId, segment);
        if (path is null || !File.Exists(path))
        {
            return RecordingErrors.SegmentNotFound.ToHttpResult();
        }

        return TypedResults.PhysicalFile(path, "video/mp4", enableRangeProcessing: true);
    }

    private static string SegmentUrl(Guid cameraId, string fileName) =>
        $"/api/recordings/{cameraId}/{Uri.EscapeDataString(fileName)}";

    // The newest file is still growing while the camera publishes with recording on.
    private static async Task<(IReadOnlyList<RecordingSegment> Segments, bool NewestInProgress)?> CameraSegmentsAsync(
        Guid cameraId,
        IDbContextFactory<RecamDbContext> databaseFactory,
        RecordingStore store,
        DevicePresence presence,
        CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var camera = await database.Devices.AsNoTracking()
            .SingleOrDefaultAsync(
                device => device.Id == cameraId && device.Role == DeviceRole.Camera && device.RevokedAt == null,
                cancellationToken);
        if (camera is null)
        {
            return null;
        }

        var segments = store.ListSegments().Where(segment => segment.CameraId == cameraId).ToList();
        return (segments, camera.RecordingEnabled && presence.IsPublishing(cameraId));
    }

    private static async Task<Ok<QuotaResponse>> GetQuotaAsync(
        IDbContextFactory<RecamDbContext> databaseFactory, RecordingStore store, CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var quota = await RecordingQuotas.LoadAsync(database, cancellationToken);
        return TypedResults.Ok(new QuotaResponse(quota.Megabytes, UsedBytes(store), store.FreeBytes()));
    }

    private static async Task<IResult> SetQuotaAsync(
        QuotaRequest request,
        IDbContextFactory<RecamDbContext> databaseFactory,
        RecordingStore store,
        CancellationToken cancellationToken)
    {
        var validation = QuotaRequestValidator.Validate(request);
        if (validation.IsFailure)
        {
            return validation.Error.ToHttpResult();
        }

        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var quota = await RecordingQuotas.LoadAsync(database, cancellationToken);
        var changed = quota.ChangeTo(validation.Value, UsedBytes(store), store.FreeBytes());
        if (changed.IsFailure)
        {
            return changed.Error.ToHttpResult();
        }

        await database.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static long UsedBytes(RecordingStore store) => store.ListSegments().Sum(segment => segment.Bytes);
}
