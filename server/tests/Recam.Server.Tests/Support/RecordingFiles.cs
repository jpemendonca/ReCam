namespace Recam.Server.Tests.Support;

public static class RecordingFiles
{
    /// <summary>
    /// Writes a segment file as MediaMTX names it. Sparse: its length counts, but it takes no
    /// disk, so tests can go past a quota of hundreds of megabytes.
    /// </summary>
    public static string Write(string recordingsDirectory, Guid cameraId, DateTimeOffset startsAt, long bytes)
    {
        var folder = Path.Combine(recordingsDirectory, $"rec-{cameraId:N}");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{startsAt.UtcDateTime:yyyy-MM-dd_HH-mm-ss-ffffff}.mp4");
        using var file = File.Create(path);
        file.SetLength(bytes);
        return path;
    }

    /// <summary>Writes the motion service's scores next to a segment: "seconds fraction" per line.</summary>
    public static void WriteMotion(string segmentPath, params (double Seconds, double Changed)[] samples) =>
        File.WriteAllLines(
            segmentPath + ".motion",
            samples.Select(sample => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{sample.Seconds:0.0} {sample.Changed:0.0000}")));

    /// <summary>Marks the detect service as last seen at <paramref name="at"/>.</summary>
    public static void WriteDetectHeartbeat(string recordingsDirectory, DateTimeOffset at)
    {
        var heartbeat = Path.Combine(recordingsDirectory, ".detect");
        File.WriteAllText(heartbeat, string.Empty);
        File.SetLastWriteTimeUtc(heartbeat, at.UtcDateTime);
    }
}
