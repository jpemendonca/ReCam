using Recam.Web.Recordings;

namespace Recam.Web.Tests.Recordings;

public sealed class TimelineWindowTests
{
    private static readonly DateTime Day = new(2026, 9, 25);

    [Fact(DisplayName = "The window is centered on the time asked for")]
    public void Around_Center_CentersTheWindow()
    {
        // act
        var window = TimelineWindow.Around(Day, TimelineZoom.Hour, Day.AddHours(14.5));

        // assert
        Assert.Equal((Day.AddHours(14), Day.AddHours(15)), (window.Start, window.End));
    }

    [Fact(DisplayName = "Near midnight the window stays inside the day")]
    public void Around_NearMidnight_StaysInsideTheDay()
    {
        // act
        var first = TimelineWindow.Around(Day, TimelineZoom.ThreeHours, Day.AddMinutes(20));
        var last = TimelineWindow.Around(Day, TimelineZoom.ThreeHours, Day.AddHours(23.8));

        // assert
        Assert.Equal(Day, first.Start);
        Assert.Equal(Day.AddDays(1), last.End);
        Assert.True(first.AtDayStart && last.AtDayEnd);
    }

    [Theory(DisplayName = "A quarter of the bar is a quarter of the zoom's span")]
    [InlineData("minute", 15)]
    [InlineData("quarter-hour", 225)]
    [InlineData("hour", 900)]
    [InlineData("three-hours", 2700)]
    public void TimeAt_Quarter_IsAQuarterOfTheSpan(string zoomName, int seconds)
    {
        // arrange
        var zoom = TimelineZoom.All.Single(candidate => candidate.Name == zoomName);
        var window = TimelineWindow.Around(Day, zoom, Day.AddHours(12) + zoom.Span / 2);

        // act
        var time = window.TimeAt(0.25);

        // assert
        Assert.Equal(Day.AddHours(12).AddSeconds(seconds), time);
        Assert.Equal(0.25, window.FractionOf(time), 6);
    }

    [Fact(DisplayName = "Dragging right shows earlier times, and never before midnight")]
    public void PanBy_Right_ShowsEarlierTimes()
    {
        // arrange
        var window = TimelineWindow.Around(Day, TimelineZoom.Hour, Day.AddHours(12.5));

        // act
        var dragged = window.PanBy(0.25);
        var tooFar = window.PanBy(100);

        // assert
        Assert.Equal(Day.AddHours(11.75), dragged.Start);
        Assert.Equal(Day, tooFar.Start);
    }

    [Fact(DisplayName = "Zooming keeps the center, and a step moves one whole window")]
    public void ZoomToAndStep_KeepCenterAndMoveOneWindow()
    {
        // arrange
        var window = TimelineWindow.Around(Day, TimelineZoom.Hour, Day.AddHours(9.5));

        // act
        var zoomed = window.ZoomTo(TimelineZoom.Minute);
        var later = window.Step(1);

        // assert
        Assert.Equal(Day.AddHours(9.5), zoomed.Center);
        Assert.Equal(TimeSpan.FromMinutes(1), zoomed.End - zoomed.Start);
        Assert.Equal(Day.AddHours(10), later.Start);
    }

    [Fact(DisplayName = "Ticks fall on round times inside the window")]
    public void Ticks_Hour_FallOnRoundTimes()
    {
        // arrange
        var window = TimelineWindow.Around(Day, TimelineZoom.Hour, Day.AddHours(14).AddMinutes(35));

        // act
        var ticks = window.Ticks;

        // assert
        Assert.Equal(Day.AddHours(14).AddMinutes(10), ticks[0]);
        Assert.Equal(Day.AddHours(15), ticks[^1]);
        Assert.Equal(6, ticks.Count);
    }
}
