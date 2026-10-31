using Recam.Web.Recordings;

namespace Recam.Web.Tests.Recordings;

public sealed class TimelineWindowTests
{
    private static readonly DateTime Day = new(2026, 9, 25);

    [Fact(DisplayName = "The window spreads the zoom's span evenly around its center")]
    public void Around_Center_SpreadsAroundIt()
    {
        // act
        var window = TimelineWindow.Around(Day, TimelineZoom.HalfHour, Day.AddHours(11).AddMinutes(15));

        // assert
        Assert.Equal((Day.AddHours(11), Day.AddHours(11).AddMinutes(30)), (window.Start, window.End));
        Assert.Equal(0.5, window.FractionOf(window.Center), 6);
    }

    [Fact(DisplayName = "Near midnight the center stays within the day, and the window may reach past it")]
    public void Around_NearMidnight_KeepsTheCenterInTheDay()
    {
        // act
        var early = TimelineWindow.Around(Day, TimelineZoom.Hour, Day.AddMinutes(-20));
        var late = TimelineWindow.Around(Day, TimelineZoom.Hour, Day.AddHours(23.9));

        // assert
        Assert.Equal(Day, early.Center);
        Assert.Equal(Day.AddMinutes(-30), early.Start);
        Assert.Equal(Day.AddHours(24.4), late.End);
    }

    [Theory(DisplayName = "A quarter of the bar is a quarter of the zoom's span")]
    [InlineData("minute", 15)]
    [InlineData("five-minutes", 75)]
    [InlineData("half-hour", 450)]
    [InlineData("hour", 900)]
    [InlineData("day", 21600)]
    public void TimeAt_Quarter_IsAQuarterOfTheSpan(string zoomName, int seconds)
    {
        // arrange
        var zoom = TimelineZoom.All.Single(candidate => candidate.Name == zoomName);
        var window = TimelineWindow.Around(Day, zoom, Day.AddHours(12));

        // act
        var time = window.TimeAt(0.75);

        // assert
        Assert.Equal(Day.AddHours(12).AddSeconds(seconds), time);
        Assert.Equal(0.75, window.FractionOf(time), 6);
    }

    [Fact(DisplayName = "Dragging right shows earlier times, and never moves the center before midnight")]
    public void PanBy_Right_ShowsEarlierTimes()
    {
        // arrange
        var window = TimelineWindow.Around(Day, TimelineZoom.Hour, Day.AddHours(12.5));

        // act
        var dragged = window.PanBy(0.25);
        var tooFar = window.PanBy(100);

        // assert
        Assert.Equal(Day.AddHours(12.25), dragged.Center);
        Assert.Equal(Day, tooFar.Center);
    }

    [Fact(DisplayName = "Zooming keeps the center")]
    public void ZoomTo_KeepsTheCenter()
    {
        // arrange
        var window = TimelineWindow.Around(Day, TimelineZoom.Hour, Day.AddHours(9.5));

        // act
        var zoomed = window.ZoomTo(TimelineZoom.Minute);

        // assert
        Assert.Equal(Day.AddHours(9.5), zoomed.Center);
        Assert.Equal(TimeSpan.FromMinutes(1), zoomed.End - zoomed.Start);
    }

    [Fact(DisplayName = "Ticks fall on round times inside the window, and none past midnight")]
    public void Ticks_FallOnRoundTimesWithinTheDay()
    {
        // arrange
        var window = TimelineWindow.Around(Day, TimelineZoom.Hour, Day.AddHours(14).AddMinutes(35));
        var late = TimelineWindow.Around(Day, TimelineZoom.Hour, Day.AddHours(23).AddMinutes(50));

        // act
        var ticks = window.Ticks;

        // assert
        Assert.Equal(Day.AddHours(14).AddMinutes(10), ticks[0]);
        Assert.Equal(Day.AddHours(15), ticks[^1]);
        Assert.Equal(6, ticks.Count);
        Assert.Equal(Day.AddDays(1), late.Ticks[^1]);
    }
}
