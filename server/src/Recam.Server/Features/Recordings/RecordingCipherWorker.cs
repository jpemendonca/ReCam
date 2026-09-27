using Recam.Server.Infrastructure.Recordings;

namespace Recam.Server.Features.Recordings;

/// <summary>
/// Ciphers recording files once nothing else needs them plain: after the motion service scored
/// them, or after <see cref="GiveUpOnMotion"/> when it does not run. Old recordings from before
/// the cipher existed are picked up the same way.
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

    private readonly HashSet<string> _ciphered = [];

    public void CipherClosed()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var segment in store.ListSegments())
        {
            if (store.PathOf(segment.CameraId, segment.FileName) is not { } path || _ciphered.Contains(path))
            {
                continue;
            }

            if (!File.Exists(path + RecordingStore.MotionSuffix) && now - segment.StartsAt < GiveUpOnMotion)
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
