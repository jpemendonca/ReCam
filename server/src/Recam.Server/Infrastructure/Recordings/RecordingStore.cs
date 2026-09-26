using System.Globalization;
using System.Text.RegularExpressions;
using Recam.Server.Domain;
using Recam.Server.Infrastructure.Hosting;

namespace Recam.Server.Infrastructure.Recordings;

/// <summary>
/// The recordings folder MediaMTX writes to: <c>rec-{camera}/AAAA-MM-DD_HH-MM-SS-ffffff.mp4</c>,
/// times in UTC (SPECS.md 2.4). Anything else in the folder is ignored and never deleted.
/// </summary>
public sealed partial class RecordingStore(ServerSettings settings)
{
    public const string FileTimeFormat = "yyyy-MM-dd_HH-mm-ss-ffffff";

    /// <summary>The motion service writes a segment's scores next to it, with this suffix.</summary>
    public const string MotionSuffix = ".motion";

    public string Directory => settings.RecordingsDirectory;

    public IReadOnlyList<RecordingSegment> ListSegments()
    {
        var root = new DirectoryInfo(Directory);
        if (!root.Exists)
        {
            return [];
        }

        var segments = new List<RecordingSegment>();
        foreach (var cameraDirectory in root.EnumerateDirectories())
        {
            if (TryParseCamera(cameraDirectory.Name) is not { } cameraId)
            {
                continue;
            }

            foreach (var file in cameraDirectory.EnumerateFiles())
            {
                if (TryParseStart(file.Name) is { } startsAt)
                {
                    segments.Add(new RecordingSegment(cameraId, file.Name, startsAt, file.Length));
                }
            }
        }

        return segments;
    }

    /// <summary>Free space on the disk that holds the recordings.</summary>
    public long FreeBytes()
    {
        System.IO.Directory.CreateDirectory(Directory);
        return new DriveInfo(Path.GetFullPath(Directory)).AvailableFreeSpace;
    }

    /// <summary>The file of a segment, or null when the name is not a segment name.</summary>
    public string? PathOf(Guid cameraId, string fileName) =>
        TryParseStart(fileName) is null ? null : Path.Combine(Directory, CameraFolder(cameraId), fileName);

    /// <summary>Deletes the file and its motion scores.</summary>
    public void Delete(RecordingSegment segment)
    {
        if (PathOf(segment.CameraId, segment.FileName) is { } path)
        {
            File.Delete(path);
            File.Delete(path + MotionSuffix);
        }
    }

    /// <summary>
    /// The motion service's scores for a segment (deploy/motion.sh): one line per half second,
    /// "seconds fraction". Empty until the service scored it.
    /// </summary>
    public IReadOnlyList<MotionSample> ReadMotion(RecordingSegment segment)
    {
        if (PathOf(segment.CameraId, segment.FileName) is not { } path || !File.Exists(path + MotionSuffix))
        {
            return [];
        }

        var samples = new List<MotionSample>();
        foreach (var line in File.ReadLines(path + MotionSuffix))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2
                && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var changed))
            {
                samples.Add(new MotionSample(segment.StartsAt.AddSeconds(seconds), changed));
            }
        }

        return samples;
    }

    /// <summary>Removes motion scores whose segment is gone, deleted while it was being scored.</summary>
    public void DeleteOrphanMotion()
    {
        var root = new DirectoryInfo(Directory);
        if (!root.Exists)
        {
            return;
        }

        foreach (var cameraDirectory in root.EnumerateDirectories().Where(folder => TryParseCamera(folder.Name) is not null))
        {
            foreach (var scores in cameraDirectory.EnumerateFiles("*" + MotionSuffix))
            {
                if (!File.Exists(scores.FullName[..^MotionSuffix.Length]))
                {
                    scores.Delete();
                }
            }
        }
    }

    /// <summary>Removes camera folders left empty, such as those of removed cameras.</summary>
    public void DeleteEmptyCameraFolders()
    {
        var root = new DirectoryInfo(Directory);
        if (!root.Exists)
        {
            return;
        }

        foreach (var cameraDirectory in root.EnumerateDirectories())
        {
            if (TryParseCamera(cameraDirectory.Name) is not null && !cameraDirectory.EnumerateFileSystemInfos().Any())
            {
                cameraDirectory.Delete();
            }
        }
    }

    public static string CameraFolder(Guid cameraId) => $"rec-{cameraId:N}";

    public static DateTimeOffset? TryParseStart(string fileName) =>
        SegmentName().IsMatch(fileName)
        && DateTime.TryParseExact(
            fileName[..^".mp4".Length],
            FileTimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var startsAt)
            ? new DateTimeOffset(startsAt, TimeSpan.Zero)
            : null;

    private static Guid? TryParseCamera(string folderName) =>
        CameraFolderName().IsMatch(folderName) ? Guid.ParseExact(folderName["rec-".Length..], "N") : null;

    [GeneratedRegex("^rec-[0-9a-f]{32}$")]
    private static partial Regex CameraFolderName();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}-\d{6}\.mp4$")]
    private static partial Regex SegmentName();
}
