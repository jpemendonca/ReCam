using Microsoft.EntityFrameworkCore;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Persistence;
using Recam.Server.Infrastructure.Recordings;

namespace Recam.Server.Features.Recordings;

/// <summary>Every minute, deletes what does not fit in the quota and what removed cameras left.</summary>
public sealed partial class RecordingCleanupWorker(
    IDbContextFactory<RecamDbContext> databaseFactory,
    RecordingStore store,
    TimeProvider timeProvider,
    ILogger<RecordingCleanupWorker> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    public async Task CleanAsync(CancellationToken cancellationToken)
    {
        await using var database = await databaseFactory.CreateDbContextAsync(cancellationToken);
        var quota = await RecordingQuotas.LoadAsync(database, cancellationToken);
        var activeCameras = (await database.Devices
            .Where(device => device.Role == DeviceRole.Camera && device.RevokedAt == null)
            .Select(device => device.Id)
            .ToListAsync(cancellationToken)).ToHashSet();

        var toDelete = quota.PlanCleanup(store.ListSegments(), activeCameras);
        foreach (var segment in toDelete)
        {
            store.Delete(segment);
        }

        store.DeleteOrphanNotes();
        store.DeleteEmptyCameraFolders();
        if (toDelete.Count > 0)
        {
            var bytes = toDelete.Sum(segment => segment.Bytes);
            LogDeleted(logger, toDelete.Count, bytes);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanAsync(stoppingToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted {Count} recording segments ({Bytes} bytes) to stay within the quota")]
    private static partial void LogDeleted(ILogger logger, int count, long bytes);
}
