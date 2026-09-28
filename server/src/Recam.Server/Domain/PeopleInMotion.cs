namespace Recam.Server.Domain;

/// <summary>Tells whether a motion event had a person in it, from the detect service's files (SPECS.md 2.6).</summary>
public static class PeopleInMotion
{
    /// <summary>A box counts as a person from this confidence up.</summary>
    public const double PersonConfidence = 0.5;

    /// <summary>
    /// Motion is scored every half second and people once a second, so a person just outside the
    /// event still belongs to it.
    /// </summary>
    public static readonly TimeSpan Margin = TimeSpan.FromSeconds(1);

    /// <summary>
    /// True when a person was seen during the event, false when every segment it touches was
    /// analyzed and nobody was, and null when some of it was not analyzed.
    /// </summary>
    public static bool? HasPerson(MotionEvent motion, IReadOnlyList<SegmentPeople> cameraSegments)
    {
        var ordered = cameraSegments.OrderBy(segment => segment.StartsAt).ToList();
        var from = motion.Start - Margin;
        var to = motion.End + Margin;
        var touched = ordered
            .Select((segment, index) => (Segment: segment, EndsAt: EndOf(ordered, index)))
            .Where(entry => entry.Segment.StartsAt <= motion.End && entry.EndsAt > motion.Start)
            .Select(entry => entry.Segment)
            .ToList();

        var seen = ordered
            .SelectMany(segment => segment.Samples ?? [])
            .Any(sample => sample.At >= from && sample.At <= to && sample.People.Any(IsPerson));
        if (seen)
        {
            return true;
        }

        return touched.Count > 0 && touched.All(segment => segment.Samples is not null) ? false : null;
    }

    public static bool IsPerson(PersonBox box) => box.Confidence >= PersonConfidence;

    private static DateTimeOffset EndOf(List<SegmentPeople> ordered, int index)
    {
        var fullEnd = ordered[index].StartsAt + RecordingTimeline.SegmentLength;
        return index + 1 < ordered.Count && ordered[index + 1].StartsAt < fullEnd ? ordered[index + 1].StartsAt : fullEnd;
    }
}
