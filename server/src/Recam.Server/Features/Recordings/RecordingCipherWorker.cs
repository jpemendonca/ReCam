using Recam.Server.Infrastructure.Recordings;

namespace Recam.Server.Features.Recordings;

/// <summary>
/// Ciphers recording files once nothing else needs them plain: after the motion service scored
/// them and, while the optional detect service runs, after it looked for people in them; or after
/// <see cref="GiveUpOnMotion"/> when they do not keep up. Old recordings from before the cipher
/// existed are picked up the same way.
/// </summary>
public sealed partial class RecordingCipherWorker(
    RecordingStore store,
    RecordingCipher cipher,
    TimeProvider timeProvider,
    ILogger<RecordingCipherWorker> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(20);

    /// <summary>A file this old is closed for sure; ciphered even without motion scores.</summary>
    public static readonly TimeSpan GiveUpOnMotion = TimeSpan.FromMinutes(30);

    /// <summary>The detect service touches its heartbeat at least this often while it runs.</summary>
    public static readonly TimeSpan DetectStale = TimeSpan.FromMinutes(5);

    private readonly HashSet<string> _ciphered = [];

    public void CipherClosed()
    {
        var now = timeProvider.GetUtcNow();
        var detectRuns = store.DetectLastSeen() is { } seen && now - seen < DetectStale;
        foreach (var segment in store.ListSegments())
        {
            if (store.PathOf(segment.CameraId, segment.FileName) is not { } path || _ciphered.Contains(path))
            {
                continue;
            }

            var waiting = !File.Exists(path + RecordingStore.MotionSuffix)
                || (detectRuns && !File.Exists(path + RecordingStore.PeopleSuffix));
            if (waiting && now - segment.StartsAt < GiveUpOnMotion)
            {
                continue;
            }

            try
            {
                cipher.CipherFile(path);
                _ciphered.Add(path);
            }
            catch (IOException exception)
            {
                // Deleted by the quota meanwhile, or busy: the next round tries again.
                LogSkipped(logger, segment.FileName, exception.Message);
            }
        }

        _ciphered.RemoveWhere(path => !File.Exists(path));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CipherClosed();
        using var timer = new PeriodicTimer(Interval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            CipherClosed();
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Recording {FileName} not ciphered yet: {Reason}")]
    private static partial void LogSkipped(ILogger logger, string fileName, string reason);
}
