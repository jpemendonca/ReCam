using System.Diagnostics;
using System.Globalization;

namespace Recam.Detect;

/// <summary>
/// Reads a recording segment through FFmpeg, one frame per second, already shaped the way the
/// model wants: scaled to fit a square, in its top left corner, gray around, raw BGR bytes.
/// </summary>
public static class SegmentFrames
{
    /// <summary>YOLOX pads with this gray.</summary>
    private const string Padding = "0x727272";

    public static (int Width, int Height)? Probe(string path)
    {
        var output = Run("ffprobe", ["-v", "error", "-select_streams", "v:0", "-show_entries", "stream=width,height", "-of", "csv=p=0", path]);
        var parts = output?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts is [var width, var height]
            && int.TryParse(width, CultureInfo.InvariantCulture, out var w)
            && int.TryParse(height, CultureInfo.InvariantCulture, out var h)
            && w > 0 && h > 0
                ? (w, h)
                : null;
    }

    /// <summary>The frame's size inside the model's square, the same way FFmpeg rounds it.</summary>
    public static (int Width, int Height) Scaled(int width, int height)
    {
        const double size = YoloxDecoder.InputSize;
        var ratio = Math.Min(size / width, size / height);
        return (Math.Max(1, (int)Math.Round(width * ratio)), Math.Max(1, (int)Math.Round(height * ratio)));
    }

    /// <summary>Calls <paramref name="onFrame"/> with each second's frame, in order. False when FFmpeg failed.</summary>
    public static bool Read(string path, (int Width, int Height) scaled, Action<int, ReadOnlySpan<byte>> onFrame)
    {
        const int size = YoloxDecoder.InputSize;
        var filter = FormattableString.Invariant(
            $"fps=1,format=bgr24,scale={scaled.Width}:{scaled.Height},pad={size}:{size}:0:0:color={Padding}");
        using var ffmpeg = Start("ffmpeg", ["-nostdin", "-loglevel", "error", "-i", path, "-an", "-vf", filter, "-pix_fmt", "bgr24", "-f", "rawvideo", "-"]);
        var frame = new byte[size * size * 3];
        var stream = ffmpeg.StandardOutput.BaseStream;
        var second = 0;
        while (stream.ReadAtLeast(frame, frame.Length, throwOnEndOfStream: false) == frame.Length)
        {
            onFrame(second++, frame);
        }

        ffmpeg.WaitForExit();
        return ffmpeg.ExitCode == 0;
    }

    private static string? Run(string program, string[] arguments)
    {
        using var process = Start(program, arguments);
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 ? output : null;
    }

    private static Process Start(string program, string[] arguments)
    {
        var start = new ProcessStartInfo(program) { RedirectStandardOutput = true, UseShellExecute = false };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        return Process.Start(start) ?? throw new InvalidOperationException($"Could not start {program}.");
    }
}
