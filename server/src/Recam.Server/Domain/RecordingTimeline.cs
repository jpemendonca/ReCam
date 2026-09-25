namespace Recam.Server.Domain;

/// <summary>A stretch of continuous recording, made of back-to-back segments.</summary>
public sealed record RecordingPiece(DateTimeOffset StartsAt, DateTimeOffset EndsAt, IReadOnlyList<RecordedSpan> Segments);

/// <summary>One segment as it appears on the timeline.</summary>
public sealed record RecordedSpan(string FileName, DateTimeOffset StartsAt, DateTimeOffset EndsAt);

/// <summary>Turns a camera's segment files into the stretches a timeline shows.</summary>
public static class RecordingTimeline
{
    /// <summary>MediaMTX starts a new file every minute (deploy/mediamtx.yml).</summary>
    public static readonly TimeSpan SegmentLength = TimeSpan.FromSeconds(60);

    /// <summary>Segments closer than this are one stretch; MediaMTX leaves no gap between them.</summary>
    public static readonly TimeSpan Continuity = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The stretches of one UTC day. A segment ends a minute after it starts, or when the next
    /// one starts. The newest segment is left out while it is still being written.
    /// </summary>
    public static IReadOnlyList<RecordingPiece> Build(
        IReadOnlyList<RecordingSegment> cameraSegments, DateOnly day, bool newestInProgress)
    {
        var ordered = cameraSegments.OrderBy(segment => segment.StartsAt).ToList();
        if (newestInProgress && ordered.Count > 0)
        {
            ordered.RemoveAt(ordered.Count - 1);
        }

        var dayStart = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);
        var pieces = new List<RecordingPiece>();
        List<RecordedSpan> current = [];
        for (var index = 0; index < ordered.Count; index++)
        {
            var segment = ordered[index];
            var fullEnd = segment.StartsAt + SegmentLength;
            var endsAt = index + 1 < ordered.Count && ordered[index + 1].StartsAt < fullEnd
                ? ordered[index + 1].StartsAt
                : fullEnd;
            if (segment.StartsAt < dayStart || segment.StartsAt >= dayEnd)
            {
                continue;
            }

            if (current.Count > 0 && segment.StartsAt - current[^1].EndsAt > Continuity)
            {
                pieces.Add(Piece(current));
                current = [];
            }

            current.Add(new RecordedSpan(segment.FileName, segment.StartsAt, endsAt));
        }

        if (current.Count > 0)
        {
            pieces.Add(Piece(current));
        }

        return pieces;
    }

    /// <summary>The UTC days that have at least one finished segment, newest first.</summary>
    public static IReadOnlyList<DateOnly> Days(IReadOnlyList<RecordingSegment> cameraSegments, bool newestInProgress)
    {
        var ordered = cameraSegments.OrderBy(segment => segment.StartsAt).ToList();
        if (newestInProgress && ordered.Count > 0)
        {
            ordered.RemoveAt(ordered.Count - 1);
        }

        return ordered
            .Select(segment => DateOnly.FromDateTime(segment.StartsAt.UtcDateTime))
            .Distinct()
            .OrderDescending()
            .ToList();
    }

    private static RecordingPiece Piece(List<RecordedSpan> spans) => new(spans[0].StartsAt, spans[^1].EndsAt, spans);
}
