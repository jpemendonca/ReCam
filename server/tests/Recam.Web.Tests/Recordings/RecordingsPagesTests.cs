using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Brighten;
using Recam.Web.Cameras;
using Recam.Web.Pages;
using Recam.Web.Realtime;
using Recam.Web.Recordings;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Recordings;

public sealed class RecordingsPagesTests : BunitContext
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly CameraInfo _camera = Support.Cameras.Make("Porta", recording: true);

    public RecordingsPagesTests()
    {
        _api.Cameras.Add(_camera);
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddSingleton<IDeviceHub>(new FakeDeviceHub());
        Services.AddSingleton<CameraListController>();
        Services.AddTransient(_ => new TimelineController(_api) { UtcOffsetOf = _ => TimeSpan.Zero });
        Services.AddTransient<QuotaController>();
        Services.AddSingleton<IBrightenSurface>(new FakeBrightenSurface());
        Services.AddTransient<BrightenController>();
    }

    [Fact(DisplayName = "The timeline draws the recorded stretch, and clicking the hours plays it")]
    public void Timeline_ClickHours_PlaysVideo()
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
        page.FindAll("rect.slot")[84].Click();

        // assert
        Assert.Equal("845", page.Find("rect.recorded").GetAttribute("x"));
        Assert.Equal($"api/recordings/{_camera.Id}/a.mp4#t=0", page.Find("video").GetAttribute("src"));
        Assert.Equal("Tocando a partir das 14:05", page.Find(".playing-from").TextContent);
        Assert.Equal("Gravações · Porta", page.Find("h1").TextContent);
    }

    [Fact(DisplayName = "Motion shows on the bar; Next motion plays it and Motion only hides the rest")]
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
        page.WaitForAssertion(() => Assert.Single(page.FindAll("rect.motion")));

        // act
        page.Find("button.next-motion").Click();
        var source = page.Find("video").GetAttribute("src");
        page.Find("input.only-motion").Change(true);

        // assert
        Assert.Equal($"api/recordings/{_camera.Id}/a.mp4#t=595", source);
        Assert.Equal("Movimentos neste dia: 1", page.Find(".motion-summary").TextContent);
        Assert.Empty(page.FindAll("rect.recorded"));
        var mark = page.Find("rect.motion");
        Assert.Equal(("850", "3", "36"), (mark.GetAttribute("x"), mark.GetAttribute("width"), mark.GetAttribute("height")));
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
        Assert.Empty(page.FindAll("rect.motion"));
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
    }
}
