namespace Recam.Server.Domain;

/// <summary>
/// How much disk all recordings may take together. When they take more, the oldest segments
/// go first, from any camera. A single row; a server that never changed it uses the default.
/// </summary>
public sealed class RecordingQuota
{
    public const int SingletonId = 1;
    public const int DefaultMegabytes = 2048;
    public const int MinimumMegabytes = 100;
    public const long BytesPerMegabyte = 1024 * 1024;

    private RecordingQuota()
    {
    }

    public int Id { get; private set; }

    public int Megabytes { get; private set; }

    public long Bytes => Megabytes * BytesPerMegabyte;

    public static RecordingQuota CreateDefault() => new() { Id = SingletonId, Megabytes = DefaultMegabytes };

    /// <summary>
    /// Changes the quota. It cannot ask for more than the recordings already use plus the free
    /// disk, because that space does not exist.
    /// </summary>
    public Result ChangeTo(int megabytes, long usedBytes, long freeBytes)
    {
        if (megabytes * BytesPerMegabyte > usedBytes + freeBytes)
        {
            return RecordingErrors.QuotaTooLarge;
        }

        Megabytes = megabytes;
        return Result.Success();
    }

    /// <summary>
    /// The segments to delete: everything of cameras that are gone, then the oldest until the
    /// rest fits. The newest segment of each camera stays, because MediaMTX may be writing it.
    /// </summary>
    public IReadOnlyList<RecordingSegment> PlanCleanup(
        IReadOnlyList<RecordingSegment> segments, IReadOnlySet<Guid> activeCameras)
    {
        var fromRemoved = segments.Where(segment => !activeCameras.Contains(segment.CameraId)).ToList();
        var kept = segments.Except(fromRemoved).ToList();
        var newestPerCamera = kept
            .GroupBy(segment => segment.CameraId)
            .Select(camera => camera.MaxBy(segment => segment.StartsAt)!)
            .ToHashSet();

        var toDelete = new List<RecordingSegment>(fromRemoved);
        var total = kept.Sum(segment => segment.Bytes);
        foreach (var oldest in kept.Where(segment => !newestPerCamera.Contains(segment)).OrderBy(segment => segment.StartsAt))
        {
            if (total <= Bytes)
            {
                break;
            }

            toDelete.Add(oldest);
            total -= oldest.Bytes;
        }

        return toDelete;
    }
}
