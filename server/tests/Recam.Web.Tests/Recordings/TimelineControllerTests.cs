using Recam.Web.Api;
using Recam.Web.Recordings;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Recordings;

public sealed class TimelineControllerTests
{
    // São Paulo: three hours behind UTC.
    private static readonly TimeSpan SaoPaulo = TimeSpan.FromHours(-3);

    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly Guid _cameraId = Guid.NewGuid();

    private TimelineController NewController() => new(_api) { UtcOffsetOf = _ => SaoPaulo };

    [Fact(DisplayName = "Recordings of an early UTC morning show on the previous local day, and empty days are dropped")]
    public async Task Load_EarlyUtcMorning_OpensPreviousLocalDay()
    {
        // arrange
        var utcDay = new DateOnly(2026, 9, 26);
        AddPiece(utcDay, At(utcDay, 0, 30), 2);
        var controller = NewController();

        // act
        await controller.LoadAsync(_cameraId);

        // assert
        Assert.Equal([new DateOnly(2026, 9, 25)], controller.Days);
        Assert.Equal(new DateOnly(2026, 9, 25), controller.SelectedDay);
        Assert.Equal(2, controller.Timeline.Count);
        Assert.Equal(new DateTime(2026, 9, 25, 21, 30, 0), controller.Timeline[0].Start);
    }

    [Fact(DisplayName = "A local day asks the server for the two UTC days it spans")]
    public async Task SelectDay_AsksBothUtcDays()
    {
        // arrange
        var controller = NewController();

        // act
        await controller.SelectDayAsync(new DateOnly(2026, 9, 25));

        // assert
        Assert.Equal([new DateOnly(2026, 9, 25), new DateOnly(2026, 9, 26)], _api.RecordingDaysAsked);
    }

    [Fact(DisplayName = "Playing from inside a file starts at that point; from a gap, at the next file")]
    public async Task PlayAt_InsideAndInGap_StartsRightPlace()
    {
        // arrange
        var utcDay = new DateOnly(2026, 9, 25);
        AddPiece(utcDay, At(utcDay, 15, 0), 2);
        var controller = NewController();
        await controller.LoadAsync(_cameraId);

        // act
        controller.PlayAt(new DateTime(2026, 9, 25, 12, 0, 30));
        var inside = (controller.Source, controller.PlayingFrom);
        controller.PlayAt(new DateTime(2026, 9, 25, 11, 0, 0));

        // assert
        Assert.EndsWith("#t=30", inside.Source, StringComparison.Ordinal);
        Assert.Equal(new DateTime(2026, 9, 25, 12, 0, 30), inside.PlayingFrom);
        Assert.EndsWith("#t=0", controller.Source, StringComparison.Ordinal);
        Assert.Same(controller.Timeline[0], controller.Playing);
    }

    [Fact(DisplayName = "When a file ends, the next one plays from its start")]
    public async Task PlayNext_AfterFirst_PlaysSecond()
    {
        // arrange
        var utcDay = new DateOnly(2026, 9, 25);
        AddPiece(utcDay, At(utcDay, 15, 0), 2);
        var controller = NewController();
        await controller.LoadAsync(_cameraId);
        controller.PlayAt(new DateTime(2026, 9, 25, 12, 0, 0));

        // act
        controller.PlayNext();

        // assert
        Assert.Same(controller.Timeline[1], controller.Playing);
        Assert.Equal($"{controller.Timeline[1].Url.TrimStart('/')}#t=0", controller.Source);
    }

    [Fact(DisplayName = "Without the server, the timeline says it could not load")]
    public async Task Load_Offline_Failed()
    {
        // arrange
        _api.Offline = true;
        var controller = NewController();

        // act
        await controller.LoadAsync(_cameraId);

        // assert
        Assert.True(controller.Failed);
        Assert.False(controller.Loading);
    }

    private static DateTimeOffset At(DateOnly day, int hour, int minute) =>
        new(day.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero);

    private void AddPiece(DateOnly utcDay, DateTimeOffset start, int minutes)
    {
        var segments = Enumerable.Range(0, minutes)
            .Select(minute => new RecordingSegmentInfo(
                start.AddMinutes(minute), start.AddMinutes(minute + 1), $"/api/recordings/{_cameraId}/segment-{minute}.mp4"))
            .ToList();
        _api.Recordings[utcDay] = [new RecordingPieceInfo(start, start.AddMinutes(minutes), segments)];
    }
}
