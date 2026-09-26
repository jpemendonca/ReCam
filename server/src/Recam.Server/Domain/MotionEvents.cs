namespace Recam.Server.Domain;

/// <summary>
/// Turns the motion service's scores into events. The sensitivity is applied here, when asked,
/// so changing it also changes what was already recorded, without scoring the video again.
/// </summary>
public static class MotionEvents
{
    /// <summary>Samples closer than this belong to the same event.</summary>
    public static readonly TimeSpan MergeGap = TimeSpan.FromSeconds(10);

    /// <summary>The picture jumps when the flashlight switches; that is not motion.</summary>
    public static readonly TimeSpan TorchQuiet = TimeSpan.FromSeconds(5);

    /// <summary>Fraction of the picture that must change. Measured on test scenes: sensor noise
    /// gives 0, a person-sized shape walking across gives about 0.05.</summary>
    public static double Threshold(MotionSensitivity sensitivity) => sensitivity switch
    {
        MotionSensitivity.High => 0.003,
        MotionSensitivity.Medium => 0.01,
        MotionSensitivity.Low => 0.03,
        _ => throw new ArgumentOutOfRangeException(nameof(sensitivity), sensitivity, null),
    };

    public static IReadOnlyList<MotionEvent> Find(
        IEnumerable<MotionSample> samples, MotionSensitivity sensitivity, IReadOnlyCollection<DateTimeOffset> torchChanges)
    {
        var threshold = Threshold(sensitivity);
        var moving = samples
            .Where(sample => sample.Changed >= threshold)
            .Where(sample => !torchChanges.Any(change => sample.At >= change && sample.At <= change + TorchQuiet))
            .OrderBy(sample => sample.At);

        var events = new List<MotionEvent>();
        foreach (var sample in moving)
        {
            if (events.Count > 0 && sample.At - events[^1].End <= MergeGap)
            {
                var last = events[^1];
                events[^1] = last with { End = sample.At, Peak = Math.Max(last.Peak, sample.Changed) };
            }
            else
            {
                events.Add(new MotionEvent(sample.At, sample.At, sample.Changed));
            }
        }

        return events;
    }
}
