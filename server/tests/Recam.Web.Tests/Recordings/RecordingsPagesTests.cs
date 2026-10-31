using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Brighten;
using Recam.Web.Cameras;
using Recam.Web.Localization;
using Recam.Web.Pages;
using Recam.Web.Realtime;
using Recam.Web.Recordings;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Recordings;

public sealed class RecordingsPagesTests : BunitContext
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly CameraInfo _camera = Support.Cameras.Make("Porta", recording: true);
    private readonly FakeVideoClock _clock = new();
    private readonly FakePeopleOverlay _overlay = new();
    private readonly FakePeopleBoxesStore _peopleBoxes = new();
    private readonly FakeTimelineGestures _gestures = new();

    // Far from the recordings below, so no stretch is still growing unless a test says so.
    private readonly FixedTimeProvider _time = new(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));

    public RecordingsPagesTests()
    {
        _api.Cameras.Add(_camera);
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddSingleton<IDeviceHub>(new FakeDeviceHub());
        Services.AddSingleton<CameraListController>();
        Services.AddSingleton<ThumbnailController>();
        Services.AddTransient(_ => new TimelineController(_api, _peopleBoxes) { UtcOffsetOf = _ => TimeSpan.Zero });
        Services.AddSingleton<IPeopleOverlay>(_overlay);
        Services.AddTransient<QuotaController>();
        Services.AddSingleton<IVideoClock>(_clock);
        Services.AddSingleton<ITimelineGestures>(_gestures);
        Services.AddSingleton<TimeProvider>(_time);
        Services.AddSingleton<ILanguageStore>(new FakeLanguageStore());
        Services.AddSingleton<IBrightenSurface>(new FakeBrightenSurface());
        Services.AddTransient<BrightenController>();
    }

    [Fact(DisplayName = "The timeline draws the recorded stretch, and tapping the bar plays from there")]
    public void Timeline_TapBar_PlaysVideo()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 5)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(1),
            [new RecordingSegmentInfo(start, start.AddMinutes(1), $"/api/recordings/{_camera.Id}/a.mp4")])];
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Single(page.FindAll("rect.recorded")));

        // act
        // The 30 minute window opens centered on the latest recording's end, 13:51 to 14:21.
        _gestures.Tap(14.51 / 30);

        // assert
        Assert.Equal(1, _gestures.Attached);
        page.WaitForAssertion(() => Assert.Equal($"api/recordings/{_camera.Id}/a.mp4#t=30", page.Find("video").GetAttribute("src")));
        Assert.Equal("Tocando a partir das 14:05", page.Find(".playing-from").TextContent);
        // Playback moved the center to 14:05:30, so the stretch starts just left of the middle.
        Assert.Equal("483", page.Find("rect.recorded").GetAttribute("x"));
        Assert.Equal("Gravações · Porta", page.Find("h1").TextContent);
    }

    [Fact(DisplayName = "While the days and the chosen day load slowly, the timeline waits instead of breaking")]
    public async Task Timeline_SlowDay_ShowsLoadingThenBar()
    {
        // arrange
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 5)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(1),
            [new RecordingSegmentInfo(start, start.AddMinutes(1), $"/api/recordings/{_camera.Id}/a.mp4")])];
        _api.DaysHeld = new TaskCompletionSource();
        _api.RecordingsHeld = new TaskCompletionSource();
        await Services.GetRequiredService<CameraListController>().StartAsync(Xunit.TestContext.Current.CancellationToken);
        var page = Render<RecordingsPage>();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".loading")));

        // act
        _api.DaysHeld.SetResult();
        page.WaitForAssertion(() => Assert.Single(_api.RecordingDaysAsked));
        _api.RecordingsHeld.SetResult();

        // assert
        page.WaitForAssertion(() => Assert.Single(page.FindAll("rect.recorded")));
    }

    [Fact(DisplayName = "The clock over the recording starts from the file playing, and from the next file when it ends")]
    public void Timeline_Clock_FollowsEachFile()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 5)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(2),
        [
            new RecordingSegmentInfo(start, start.AddMinutes(1), $"/api/recordings/{_camera.Id}/a.mp4"),
            new RecordingSegmentInfo(start.AddMinutes(1), start.AddMinutes(2), $"/api/recordings/{_camera.Id}/b.mp4"),
        ])];
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("rect.recorded").Count));
        // The 30 minute window opens centered on 14:07, 13:52 to 14:22; 13.5 minutes in is 14:05:30.
        _gestures.Tap(13.51 / 30);

        // act
        page.Find("video").TriggerEvent("onended", EventArgs.Empty);

        // assert
        page.WaitForAssertion(() => Assert.Equal(2, _clock.Followed.Count));
        Assert.Equal([new DateTime(2026, 9, 25, 14, 5, 0), new DateTime(2026, 9, 25, 14, 6, 0)], _clock.Followed);
        Assert.NotNull(page.Find(".player .video-clock"));
    }

    [Fact(DisplayName = "The recordings run under the fixed middle line as the video plays")]
    public void Timeline_Playing_RecordingsRunUnderTheMiddleLine()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 5)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(1),
            [new RecordingSegmentInfo(start, start.AddMinutes(1), $"/api/recordings/{_camera.Id}/a.mp4")])];
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Single(page.FindAll("rect.recorded")));
        // 5 minutes around 14:06; a quarter in is 14:04:45, a gap, so the recording plays from 14:05.
        page.FindAll("button.zoom")[1].Click();
        _gestures.Tap(0.25);
        page.WaitForAssertion(() => Assert.Single(page.FindAll("video")));
        var x = page.Find("rect.recorded").GetAttribute("x");

        // act
        _clock.PlayTo(new DateTime(2026, 9, 25, 14, 5, 40));

        // assert
        page.WaitForAssertion(() => Assert.Equal("25/09/2026 14:05:40", page.Find(".center-time").TextContent));
        Assert.Equal("500", x);
        Assert.Equal("366.67", page.Find("rect.recorded").GetAttribute("x"));
        Assert.Equal("500", page.Find("line.playhead").GetAttribute("x1"));
    }

    [Fact(DisplayName = "Dragging moves the moment under the middle line, and letting go plays from it")]
    public void Timeline_DragAndDrop_PlaysFromTheCenter()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(30),
            [new RecordingSegmentInfo(start, start.AddMinutes(30), $"/api/recordings/{_camera.Id}/a.mp4")])];
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Equal("25/09/2026 14:30:00", page.Find(".center-time").TextContent));
        page.FindAll("button.zoom")[1].Click();

        // act
        _gestures.Drag(0.2);
        page.WaitForAssertion(() => Assert.Equal("25/09/2026 14:29:00", page.Find(".center-time").TextContent));
        _clock.PlayTo(new DateTime(2026, 9, 25, 14, 0, 5));
        var whileDragging = page.Find(".center-time").TextContent;
        _gestures.Drop(0.4);

        // assert
        Assert.Equal("25/09/2026 14:29:00", whileDragging);
        page.WaitForAssertion(() => Assert.Equal($"api/recordings/{_camera.Id}/a.mp4#t=1680", page.Find("video").GetAttribute("src")));
        Assert.Equal("25/09/2026 14:28:00", page.Find(".center-time").TextContent);
    }

    [Fact(DisplayName = "While the camera records, the newest stretch grows to now, and Live opens the live view")]
    public void Timeline_Recording_GrowsToNow()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(5),
            [new RecordingSegmentInfo(start, start.AddMinutes(5), $"/api/recordings/{_camera.Id}/a.mp4")])];
        _time.Now = start.AddMinutes(6);

        // act
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));

        // assert
        // Centered on now, 14:06: the stretch runs from 14:00 (one fifth of the bar) to the middle.
        page.WaitForAssertion(() => Assert.Equal("200", page.Find("rect.recorded").GetAttribute("width")));
        Assert.Equal("300", page.Find("rect.recorded").GetAttribute("x"));
        Assert.Equal($"cameras/{_camera.Id}", page.Find("a.go-live").GetAttribute("href"));
    }

    [Fact(DisplayName = "The timeline places its hour labels with attributes, since the CSP refuses inline styles")]
    public void Timeline_Labels_NoInlineStyle()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 5)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(1),
            [new RecordingSegmentInfo(start, start.AddMinutes(1), $"/api/recordings/{_camera.Id}/a.mp4")])];

        // act
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));

        // assert
        page.WaitForAssertion(() => Assert.Single(page.FindAll("rect.recorded")));
        Assert.Empty(page.FindAll("[style]"));
        var labels = page.FindAll("svg.ticks text");
        Assert.NotEmpty(labels);
        Assert.All(labels, label => Assert.EndsWith("%", label.GetAttribute("x"), StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Motion shows as a grey event above the bar, and Next motion plays it")]
    public void Timeline_Motion_MarkedAndPlayed()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(20),
            [new RecordingSegmentInfo(start, start.AddMinutes(20), $"/api/recordings/{_camera.Id}/a.mp4")])];
        _api.Motion[day] = [new MotionEventInfo(start.AddMinutes(10), start.AddMinutes(13), 0.04)];
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Single(page.FindAll("g.event")));

        // act
        page.Find("button.next-motion").Click();
        var source = page.Find("video").GetAttribute("src");

        // assert
        Assert.Equal($"api/recordings/{_camera.Id}/a.mp4#t=595", source);
        Assert.Equal("Movimentos neste dia: 1", page.Find(".motion-summary").TextContent);
        Assert.Empty(page.FindAll("g.event.person"));
        Assert.Empty(page.FindAll("g.event svg.icon"));
        var pill = page.Find("g.event rect.pill");
        // Playing from 14:09:55 centers the 30 minute window there, 13:54:55 to 14:24:55.
        Assert.Equal(("50.28%", "10%"), (pill.GetAttribute("x"), pill.GetAttribute("width")));
        Assert.Equal("14:10:00 · Movimento", page.Find("g.event title").TextContent);
    }

    [Fact(DisplayName = "A person shows as a yellow event with the icon; tapping plays it and People only hides the rest")]
    public void Timeline_People_MarkedAndPlayed()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(20),
            [new RecordingSegmentInfo(start, start.AddMinutes(20), $"/api/recordings/{_camera.Id}/a.mp4")])];
        _api.Motion[day] =
        [
            new MotionEventInfo(start.AddMinutes(2), start.AddMinutes(3), 0.04, Person: true),
            new MotionEventInfo(start.AddMinutes(10), start.AddMinutes(13), 0.04, Person: false),
        ];
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("g.event")));
        // The hour around 14:20 holds both events.
        page.FindAll("button.zoom")[3].Click();

        // act
        var events = (page.FindAll("g.event").Count, page.FindAll("g.event.person").Count);
        var title = page.Find("g.event.person title").TextContent;
        page.Find("g.event.person").Click();
        var source = page.Find("video").GetAttribute("src");
        page.Find("input.only-people").Change(true);

        // assert
        Assert.Equal((2, 1), events);
        Assert.Equal("14:02:00 · Pessoa", title);
        Assert.Single(page.FindAll("g.event.person svg.icon"));
        Assert.Equal($"api/recordings/{_camera.Id}/a.mp4#t=115", source);
        Assert.Equal("Movimentos neste dia: 2 · com pessoas: 1", page.Find(".motion-summary").TextContent);
        Assert.Single(page.FindAll("g.event"));
        Assert.Single(page.FindAll("rect.recorded"));
        Assert.Empty(page.FindAll(".people"));
    }

    [Fact(DisplayName = "Without the detect service, the timeline shows nothing about people")]
    public void Timeline_NotAnalyzed_ShowsNoPeople()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(20),
            [new RecordingSegmentInfo(start, start.AddMinutes(20), $"/api/recordings/{_camera.Id}/a.mp4")])];
        _api.Motion[day] = [new MotionEventInfo(start.AddMinutes(10), start.AddMinutes(13), 0.04)];

        // act
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Single(page.FindAll("g.event")));

        // assert
        Assert.Empty(page.FindAll("input.only-people"));
        Assert.Empty(page.FindAll("g.event.person"));
        Assert.Equal("Movimentos neste dia: 1", page.Find(".motion-summary").TextContent);
    }

    [Fact(DisplayName = "An analyzed day with nobody says so")]
    public void Timeline_AnalyzedNobody_SaysNobody()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(20),
            [new RecordingSegmentInfo(start, start.AddMinutes(20), $"/api/recordings/{_camera.Id}/a.mp4")])];
        _api.Motion[day] = [new MotionEventInfo(start.AddMinutes(10), start.AddMinutes(13), 0.04, Person: false)];

        // act
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Single(page.FindAll("g.event")));

        // assert
        Assert.Equal("Movimentos neste dia: 1 · com pessoas: 0", page.Find(".motion-summary").TextContent);
    }

    [Fact(DisplayName = "A file with people gets boxes over the player, which Show people turns off and on")]
    public void Timeline_FileWithPeople_DrawsBoxes()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);
        var url = $"/api/recordings/{_camera.Id}/a.mp4";
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(20), [new RecordingSegmentInfo(start, start.AddMinutes(20), url)])];
        _api.SegmentPeople[url] = new SegmentPeopleInfo([new PeopleSecondInfo(3, [new PersonBoxInfo(0.1, 0.2, 0.3, 0.4)])]);
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("rect.recorded")));

        // act
        // The 30 minute window opens centered on 14:20, 14:05 to 14:35; 5.5 minutes in is 14:10:30.
        _gestures.Tap(5.51 / 30);
        page.WaitForAssertion(() => Assert.Single(_overlay.Followed));
        var label = page.Find("input.show-people").ParentElement!.TextContent.Trim();
        page.Find("input.show-people").Change(false);
        var stopsWhenOff = _overlay.Stops;
        page.Find("input.show-people").Change(true);
        page.Find("button.full-screen").Click();

        // assert
        Assert.Equal("Mostrar pessoas", label);
        Assert.True(stopsWhenOff >= 1);
        Assert.True(_peopleBoxes.Show);
        Assert.Equal(2, _overlay.Followed.Count);
        Assert.Equal(1, _overlay.FullScreens);
        Assert.Equal("nofullscreen", page.Find("video").GetAttribute("controlslist"));
    }

    [Fact(DisplayName = "A file the detect service did not look at has no boxes and no Show people")]
    public void Timeline_FileNotAnalyzed_HasNoBoxes()
    {
        // arrange
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(20),
            [new RecordingSegmentInfo(start, start.AddMinutes(20), $"/api/recordings/{_camera.Id}/a.mp4")])];
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("rect.recorded")));

        // act
        // The 30 minute window opens centered on 14:20, 14:05 to 14:35; 5.5 minutes in is 14:10:30.
        _gestures.Tap(5.51 / 30);
        page.WaitForAssertion(() => Assert.Single(page.FindAll("video")));

        // assert
        Assert.Empty(page.FindAll("input.show-people"));
        Assert.Empty(_overlay.Followed);
    }

    [Fact(DisplayName = "Choosing another sensitivity saves it and redraws the motion")]
    public void Timeline_Sensitivity_Saved()
    {
        // arrange
        using var _ = Culture.Use("en-US");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(20),
            [new RecordingSegmentInfo(start, start.AddMinutes(20), $"/api/recordings/{_camera.Id}/a.mp4")])];
        _api.Motion[day] = [new MotionEventInfo(start.AddMinutes(10), start.AddMinutes(13), 0.04)];
        _api.MotionAfterChange[day] = [];
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Equal("medium", page.Find("select.sensitivity").GetAttribute("value")));

        // act
        page.Find("select.sensitivity").Change("low");

        // assert
        page.WaitForAssertion(() => Assert.Equal("No motion on this day.", page.Find(".motion-summary").TextContent));
        Assert.Equal("low", _api.MotionSensitivity);
        Assert.Empty(page.FindAll("g.event"));
        Assert.True(page.Find("button.next-motion").HasAttribute("disabled"));
    }

    [Fact(DisplayName = "A camera without recordings says how to start")]
    public void Timeline_NoRecordings_SaysTurnOnRecordAlways()
    {
        // arrange
        using var _ = Culture.Use("en-US");

        // act
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));

        // assert
        page.WaitForAssertion(() => Assert.Equal("No recordings yet. Turn on Record always for this camera.", page.Find(".note").TextContent));
    }

    [Fact(DisplayName = "The recordings space shows the use and saves the slider")]
    public void Space_MoveAndSave_Saved()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var page = Render<SettingsPage>();
        page.WaitForAssertion(() => Assert.Equal("Em uso: 0,3 de 2,0 GB.", page.Find(".note").TextContent));

        // act
        page.Find("#space").Input("1024");
        page.Find("section button").Click();

        // assert
        page.WaitForAssertion(() => Assert.Equal("Espaço para gravações salvo.", page.Find(".ok").TextContent));
        Assert.Equal("Espaço para gravações: 1,0 GB", page.Find("label").TextContent);
        Assert.Equal(1024, _api.Quota.QuotaMb);
    }

    [Fact(DisplayName = "Settings says the space is shared on the server and splits the hours among recording cameras")]
    public async Task Settings_TwoRecordingCameras_SplitsHours()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        _api.Cameras.Add(Support.Cameras.Make("Garagem", recording: true));
        await Services.GetRequiredService<CameraListController>().StartAsync(Xunit.TestContext.Current.CancellationToken);

        // act
        var page = Render<SettingsPage>();

        // assert
        page.WaitForAssertion(() => Assert.Equal("Com 2 câmeras gravando, cabem cerca de 3,4 horas.", page.Find(".hours").TextContent));
        Assert.Equal(
            "Este é o total de todas as câmeras juntas. As gravações ficam no servidor, não nos celulares.",
            page.Find(".scope").TextContent);
    }

    [Fact(DisplayName = "The Recordings tab shows the recording camera's timeline and switches camera")]
    public async Task Recordings_PickCamera_ShowsItsTimeline()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var garage = Support.Cameras.Make("Garagem");
        _api.Cameras.Insert(0, garage);
        await Services.GetRequiredService<CameraListController>().StartAsync(Xunit.TestContext.Current.CancellationToken);
        var page = Render<RecordingsPage>();
        page.WaitForAssertion(() => Assert.Equal([_camera.Id], _api.RecordingCamerasAsked));

        // act
        page.Find("select.camera-picker").Change(garage.Id.ToString());

        // assert
        page.WaitForAssertion(() => Assert.Equal([_camera.Id, garage.Id], _api.RecordingCamerasAsked));
        Assert.Contains("Porta · Gravando", page.FindAll("select.camera-picker option").Select(option => option.TextContent));
    }

    [Fact(DisplayName = "After the first camera, the space page starts at 2 GB and saving opens its live video")]
    public void FirstSpace_Save_OpensLiveView()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var page = Render<FirstSpacePage>(parameters => parameters.Add(first => first.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Equal("Espaço para gravações: 2,0 GB", page.Find("label").TextContent));

        // act
        page.Find("#space").Input("4096");
        page.Find("button.save").Click();

        // assert
        var navigation = Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(() => Assert.EndsWith($"/cameras/{_camera.Id}", navigation.Uri, StringComparison.Ordinal));
        Assert.Equal(4096, _api.Quota.QuotaMb);
        Assert.Equal($"cameras/{_camera.Id}", page.Find("a.skip").GetAttribute("href"));
        Assert.StartsWith("A câmera já está gravando.", page.Find("section > p").TextContent, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Each zoom fits its span in the bar around the same center; the space in use shows on top")]
    public void Timeline_Zoom_KeepsTheCenter()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(30),
            [new RecordingSegmentInfo(start, start.AddMinutes(30), $"/api/recordings/{_camera.Id}/a.mp4")])];
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Single(page.FindAll("rect.recorded")));

        // act
        var labels = page.FindAll("button.zoom").Select(button => button.TextContent).ToList();
        page.FindAll("button.zoom")[3].Click();
        var hourWidth = page.Find("rect.recorded").GetAttribute("width");
        page.FindAll("button.zoom")[4].Click();

        // assert
        Assert.Equal(["1 min", "5 min", "30 min", "1 h", "1 dia"], labels);
        Assert.Equal("500", hourWidth);
        Assert.Equal("20.83", page.Find("rect.recorded").GetAttribute("width"));
        Assert.Equal("25/09/2026 14:30", page.Find(".center-time").TextContent);
        Assert.Equal("Em uso: 0,3 de 2 GB · cabem cerca de 6,8 h", page.Find(".space").TextContent);
    }

    [Fact(DisplayName = "Next motion changes the video's file without navigating")]
    public void Timeline_NextMotionTwice_KeepsTheSameVideo()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var day = new DateOnly(2026, 9, 25);
        var start = new DateTimeOffset(day.ToDateTime(new TimeOnly(14, 0)), TimeSpan.Zero);
        _api.Recordings[day] = [new RecordingPieceInfo(start, start.AddMinutes(20),
            [new RecordingSegmentInfo(start, start.AddMinutes(20), $"/api/recordings/{_camera.Id}/a.mp4")])];
        _api.Motion[day] =
        [
            new MotionEventInfo(start.AddMinutes(5), start.AddMinutes(6), 0.04),
            new MotionEventInfo(start.AddMinutes(15), start.AddMinutes(16), 0.04),
        ];
        var page = Render<TimelinePage>(parameters => parameters.Add(timeline => timeline.CameraId, _camera.Id));
        page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("g.event").Count));
        var navigation = Services.GetRequiredService<NavigationManager>();
        var address = navigation.Uri;
        page.Find("button.next-motion").Click();

        // act
        page.Find("button.next-motion").Click();

        // assert
        var video = Assert.Single(page.FindAll("video"));
        Assert.Equal($"api/recordings/{_camera.Id}/a.mp4#t=895", video.GetAttribute("src"));
        Assert.Equal(address, navigation.Uri);
    }
}
