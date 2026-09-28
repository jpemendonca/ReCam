using System.Globalization;

namespace Recam.Detect;

/// <summary>
/// Picks the seconds worth looking at from the motion service's scores (deploy/motion.sh):
/// those where at least the high sensitivity's share of the picture changed. The server applies
/// the camera's own sensitivity later, so this must be the most permissive one.
/// </summary>
public static class MotionSeconds
{
    /// <summary>Same value as the server's high sensitivity (MotionEvents.Threshold).</summary>
    public const double Threshold = 0.003;

    public static IReadOnlySet<int> Parse(IEnumerable<string> motionLines)
    {
        var seconds = new SortedSet<int>();
        foreach (var line in motionLines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2
                && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var at)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var changed)
                && changed >= Threshold
                && at >= 0)
            {
                seconds.Add((int)Math.Floor(at));
            }
        }

        return seconds;
    }
}
