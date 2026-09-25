using Recam.Server.Domain;

namespace Recam.Server.Tests.Domain;

public sealed class RecordingTimelineTests
{
    private static readonly Guid Camera = Guid.NewGuid();
    private static readonly DateOnly Day = new(2026, 9, 25);
    private static readonly DateTimeOffset Midnight = new(2026, 9, 25, 0, 0, 0, TimeSpan.Zero);

    private static RecordingSegment At(TimeSpan offset) =>
        new(Camera, $"{offset.TotalSeconds}.mp4", Midnight + offset, 1000);

    [Fact(DisplayName = "A day without files has no stretches")]
    public void Build_WithoutSegments_ReturnsNothing()
    {
        // arrange
        RecordingSegment[] segments = [];

        // act
        var pieces = RecordingTimeline.Build(segments, Day, newestInProgress: false);

        // assert
        Assert.Empty(pieces);
    }

    [Fact(DisplayName = "Back-to-back segments make one stretch; a gap starts another")]
    public void Build_WithGap_SplitsIntoStretches()
    {
        // arrange
        RecordingSegment[] segments =
        [
            At(TimeSpan.FromHours(10)),
            At(TimeSpan.FromHours(10) + TimeSpan.FromMinutes(1)),
            At(TimeSpan.FromHours(10) + TimeSpan.FromMinutes(2)),
            At(TimeSpan.FromHours(14)),
        ];

        // act
        var pieces = RecordingTimeline.Build(segments, Day, newestInProgress: false);

        // assert
        Assert.Equal(2, pieces.Count);
        Assert.Equal(Midnight.AddHours(10), pieces[0].StartsAt);
        Assert.Equal(Midnight.AddHours(10).AddMinutes(3), pieces[0].EndsAt);
        Assert.Equal(3, pieces[0].Segments.Count);
        Assert.Equal(Midnight.AddHours(14).AddMinutes(1), pieces[1].EndsAt);
    }

    [Fact(DisplayName = "A segment cut short by a restart ends where the next one starts")]
    public void Build_WithShortSegment_EndsAtNextStart()
    {
        // arrange
        RecordingSegment[] segments = [At(TimeSpan.FromHours(8)), At(TimeSpan.FromHours(8) + TimeSpan.FromSeconds(20))];

        // act
        var piece = Assert.Single(RecordingTimeline.Build(segments, Day, newestInProgress: false));

        // assert
        Assert.Equal(Midnight.AddHours(8).AddSeconds(20), piece.Segments[0].EndsAt);
    }

    [Fact(DisplayName = "The segment still being written is left out")]
    public void Build_WithNewestInProgress_LeavesItOut()
    {
        // arrange
        RecordingSegment[] segments = [At(TimeSpan.FromHours(9)), At(TimeSpan.FromHours(9) + TimeSpan.FromMinutes(1))];

        // act
        var piece = Assert.Single(RecordingTimeline.Build(segments, Day, newestInProgress: true));

        // assert
        Assert.Single(piece.Segments);
        Assert.Equal(Midnight.AddHours(9).AddMinutes(1), piece.EndsAt);
    }

    [Fact(DisplayName = "Only segments that start on the day, in UTC, belong to it")]
    public void Build_WithOtherDays_KeepsOnlyThisDay()
    {
        // arrange
        RecordingSegment[] segments = [At(TimeSpan.FromMinutes(-1)), At(TimeSpan.FromHours(12)), At(TimeSpan.FromHours(24))];

        // act
        var piece = Assert.Single(RecordingTimeline.Build(segments, Day, newestInProgress: false));

        // assert
        Assert.Equal(Midnight.AddHours(12), piece.StartsAt);
    }

    [Fact(DisplayName = "Days with recordings come newest first")]
    public void Days_WithSegmentsOnTwoDays_ListsBoth()
    {
        // arrange
        RecordingSegment[] segments = [At(TimeSpan.FromHours(-2)), At(TimeSpan.FromHours(5)), At(TimeSpan.FromHours(6))];

        // act
        var days = RecordingTimeline.Days(segments, newestInProgress: false);

        // assert
        Assert.Equal([Day, Day.AddDays(-1)], days);
    }
}
