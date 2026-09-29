using System.Text.RegularExpressions;

namespace Recam.Detect;

/// <summary>
/// One pass over the recordings folder (SPECS.md 2.6): every segment the motion service already
/// scored and that has no <c>.people</c> file yet is looked at, only in its seconds with motion.
/// </summary>
public sealed partial class SegmentScanner(string root, Func<ReadOnlySpan<byte>, (int Width, int Height), IReadOnlyList<PersonBox>> detect)
{
    /// <summary>Touched while scanning; the server keeps segments plain while it is fresh.</summary>
    public const string HeartbeatFileName = ".detect";

    private const string MotionSuffix = ".motion";

    private readonly HashSet<string> _ciphered = [];

    public int Scan()
    {
        var folderRoot = new DirectoryInfo(root);
        if (!folderRoot.Exists)
        {
            return 0;
        }

        var heartbeat = Path.Combine(folderRoot.FullName, HeartbeatFileName);
        var written = 0;
        foreach (var folder in folderRoot.EnumerateDirectories().Where(folder => CameraFolder().IsMatch(folder.Name)))
        {
            foreach (var segment in folder.EnumerateFiles().Where(file => SegmentName().IsMatch(file.Name)).OrderBy(file => file.Name))
            {
                // Before each segment, so a long pass on a slow machine still looks alive.
                File.WriteAllText(heartbeat, string.Empty);
                if (Analyze(segment.FullName))
                {
                    written++;
                }
            }
        }

        File.WriteAllText(heartbeat, string.Empty);
        _ciphered.RemoveWhere(path => !File.Exists(path));
        return written;
    }

    private bool Analyze(string segment)
    {
        var people = segment + PeopleFile.Suffix;
        if (File.Exists(people) || !File.Exists(segment + MotionSuffix) || _ciphered.Contains(segment))
        {
            return false;
        }

        if (IsCiphered(segment))
        {
            // The server ciphered it before this worker got to it: it can no longer be read.
            _ciphered.Add(segment);
            return false;
        }

        var wanted = MotionSeconds.Parse(File.ReadLines(segment + MotionSuffix));
        var found = new List<(int Second, IReadOnlyList<PersonBox> People)>();
        if (wanted.Count > 0 && SegmentFrames.Probe(segment) is { } size)
        {
            var scaled = SegmentFrames.Scaled(size.Width, size.Height);
            SegmentFrames.Read(segment, scaled, (second, frame) =>
            {
                if (wanted.Contains(second))
                {
                    found.Add((second, detect(frame, scaled)));
                }
            });
        }

        if (!File.Exists(segment))
        {
            // The server deleted it meanwhile (quota).
            return false;
        }

        var temporary = people + ".tmp";
        File.WriteAllText(temporary, PeopleFile.Format(found));
        File.Move(temporary, people, overwrite: true);
        return true;
    }

    private static bool IsCiphered(string path)
    {
        using var file = File.OpenRead(path);
        Span<byte> start = stackalloc byte[8];
        return file.ReadAtLeast(start, start.Length, throwOnEndOfStream: false) == start.Length && start.SequenceEqual("RECAMENC"u8);
    }

    [GeneratedRegex("^rec-[0-9a-f]{32}$")]
    private static partial Regex CameraFolder();

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}_\d{2}-\d{2}-\d{2}-\d{6}\.mp4$")]
    private static partial Regex SegmentName();
}
