namespace Recam.Server.Domain;

/// <summary>
/// What is really happening with a camera's recording, as the Monitor shows it next to the
/// "Record" switch: what the server sees on disk, not what was asked for.
/// </summary>
public enum RecordingState
{
    /// <summary>"Record always" is off.</summary>
    Off,

    /// <summary>The phone has no H.264 encoder, and MediaMTX records only H.264.</summary>
    NeedsH264,

    /// <summary>On, but the camera is not connected.</summary>
    Offline,

    /// <summary>On, no files arrive, and the disk is nearly full.</summary>
    NoSpace,

    /// <summary>On, and the first file has not arrived yet.</summary>
    Starting,

    /// <summary>Files are arriving.</summary>
    Recording,

    /// <summary>On and connected, but no file arrived for a while: the video does not reach the server.</summary>
    Stalled,
}

public static class RecordingStates
{
    /// <summary>MediaMTX writes the open file every second or so; older than this, it stopped.</summary>
    public static readonly TimeSpan FreshFor = TimeSpan.FromSeconds(30);

    /// <summary>How long "Starting" may last before the Monitor is told why it does not record.</summary>
    public static readonly TimeSpan StartGrace = TimeSpan.FromSeconds(30);

    /// <summary>Below this, MediaMTX cannot open a new file.</summary>
    public const long MinimumFreeBytes = 100L * 1024 * 1024;

    /// <param name="lastWrite">When a file of this camera was last written, or null.</param>
    /// <param name="waitingSince">Since when the camera should record but no file arrives.</param>
    public static RecordingState Of(
        bool enabled, bool canRecord, bool online, long freeBytes, DateTimeOffset? lastWrite, DateTimeOffset waitingSince, DateTimeOffset now)
    {
        if (!canRecord)
        {
            return RecordingState.NeedsH264;
        }

        if (!enabled)
        {
            return RecordingState.Off;
        }

        if (!online)
        {
            return RecordingState.Offline;
        }

        if (lastWrite is { } written && now - written < FreshFor)
        {
            return RecordingState.Recording;
        }

        if (freeBytes < MinimumFreeBytes)
        {
            return RecordingState.NoSpace;
        }

        return now - waitingSince < StartGrace ? RecordingState.Starting : RecordingState.Stalled;
    }
}
