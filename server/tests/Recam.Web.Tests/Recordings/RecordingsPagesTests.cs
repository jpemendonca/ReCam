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
        var page = Render<RecordingsSpace>();
        page.WaitForAssertion(() => Assert.Equal("Em uso: 0,3 de 2,0 GB.", page.Find(".note").TextContent));

        // act
        page.Find("#space").Input("1024");
        page.Find("section button").Click();

        // assert
        page.WaitForAssertion(() => Assert.Equal("Espaço para gravações salvo.", page.Find(".ok").TextContent));
        Assert.Equal("Espaço para gravações: 1,0 GB", page.Find("label").TextContent);
        Assert.Equal(1024, _api.Quota.QuotaMb);
    }
}
