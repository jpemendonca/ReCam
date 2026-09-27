using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Brighten;
using Recam.Web.Cameras;
using Recam.Web.Live;
using Recam.Web.Pages;
using Recam.Web.Realtime;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Live;

public sealed class LiveViewTests : BunitContext
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly FakeDeviceHub _hub = new();
    private readonly FakeLiveVideo _video = new();

    public LiveViewTests()
    {
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddSingleton<IDeviceHub>(_hub);
        Services.AddSingleton<ILiveVideo>(_video);
        Services.AddSingleton<CameraListController>();
        Services.AddSingleton<SoundChoice>();
        Services.AddTransient<LiveController>();
        Services.AddSingleton<IBrightenSurface>(new FakeBrightenSurface());
        Services.AddTransient<BrightenController>();
    }

    [Fact(DisplayName = "The live view plays, shows the camera's name and switches the torch")]
    public void Render_Playing_TorchButtonCallsHub()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta", publishing: true);
        _api.Cameras.Add(camera);
        var page = Render<LiveView>(parameters => parameters.Add(view => view.CameraId, camera.Id));
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".overlay")));

        // act
        page.Find(".controls button.torch").Click();

        // assert
        Assert.Equal("Porta", page.Find("h1").TextContent);
        Assert.Contains($"SetTorch {camera.Id} True", _hub.Calls);
        Assert.Equal("Ligar lanterna", page.Find(".controls button.torch").TextContent.Trim());
    }

    [Fact(DisplayName = "When the camera reports the torch on, the button offers to turn it off")]
    public void TorchChanged_On_ButtonTurnsOff()
    {
        // arrange
        using var _ = Culture.Use("en-US");
        var camera = Support.Cameras.Make("Porta", publishing: true);
        _api.Cameras.Add(camera);
        var page = Render<LiveView>(parameters => parameters.Add(view => view.CameraId, camera.Id));
        page.WaitForAssertion(() => Assert.Contains($"WatchCamera {camera.Id}", _hub.Calls));

        // act
        _hub.SendTorch(camera.Id, true);

        // assert
        page.WaitForAssertion(() => Assert.Equal("Turn flashlight off", page.Find(".controls button.torch").TextContent.Trim()));
    }

    [Fact(DisplayName = "A refused torch shows why")]
    public void Torch_Refused_ShowsError()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta");
        _api.Cameras.Add(camera);
        _hub.TorchAccepted = false;
        var page = Render<LiveView>(parameters => parameters.Add(view => view.CameraId, camera.Id));
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".overlay")));

        // act
        page.Find(".controls button.torch").Click();

        // assert
        page.WaitForAssertion(() => Assert.Equal("Não deu para mudar a lanterna. Espere o vídeo começar.", page.Find(".error").TextContent));
    }

    [Fact(DisplayName = "With the torch already on, the live view opens offering to turn it off")]
    public void Open_TorchAlreadyOn_ButtonTurnsOff()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta", publishing: true, torchOn: true);
        _api.Cameras.Add(camera);

        // act
        var page = Render<LiveView>(parameters => parameters.Add(view => view.CameraId, camera.Id));

        // assert
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".overlay")));
        Assert.Equal("Desligar lanterna", page.Find(".controls button.torch").TextContent.Trim());
    }

    [Fact(DisplayName = "The live view starts silent, as browsers require, and a button turns the sound on and off")]
    public void Sound_Button_TogglesMuted()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta", publishing: true);
        _api.Cameras.Add(camera);
        var page = Render<LiveView>(parameters => parameters.Add(view => view.CameraId, camera.Id));
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".overlay")));
        var before = page.Find("button.sound").TextContent;

        // act
        page.Find("button.sound").Click();

        // assert
        Assert.Equal("Ativar som", before);
        Assert.False(_video.Muted);
        Assert.Equal("Tirar som", page.Find("button.sound").TextContent);
    }

    [Fact(DisplayName = "Leaving with the sound on and coming back opens with the sound on and the button to turn it off")]
    public async Task Sound_LeftOn_ComesBackOn()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta", publishing: true);
        _api.Cameras.Add(camera);
        var first = Render<LiveView>(parameters => parameters.Add(view => view.CameraId, camera.Id));
        first.WaitForAssertion(() => Assert.Empty(first.FindAll(".overlay")));
        first.Find("button.sound").Click();
        await DisposeComponentsAsync();

        // act
        var again = Render<LiveView>(parameters => parameters.Add(view => view.CameraId, camera.Id));

        // assert
        again.WaitForAssertion(() => Assert.Equal("Tirar som", again.Find("button.sound").TextContent));
        Assert.False(_video.Muted);
    }

    [Fact(DisplayName = "When the browser keeps the sound off, the button offers to turn it on instead of lying")]
    public void Sound_BrowserBlocks_ButtonTurnsItOn()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta", publishing: true);
        _api.Cameras.Add(camera);
        Services.GetRequiredService<SoundChoice>().Wanted = true;
        _video.BlocksSound = true;

        // act
        var page = Render<LiveView>(parameters => parameters.Add(view => view.CameraId, camera.Id));

        // assert
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".overlay")));
        Assert.True(_video.Muted);
        Assert.Equal("Ativar som", page.Find("button.sound").TextContent);
    }

    [Fact(DisplayName = "The live video shows the time now in a corner")]
    public void Clock_ShowsTimeNow()
    {
        // act
        var clock = Render<LiveClock>(parameters => parameters.Add(c => c.Now, () => new DateTime(2026, 9, 26, 21, 5, 9)));

        // assert
        Assert.Equal("21:05:09", clock.Find(".video-clock").TextContent);
    }

    [Fact(DisplayName = "The Diagnostics button under the video shows what the browser sees of the connection")]
    public void Diagnostics_Open_ShowsTheConnection()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta", publishing: true);
        _api.Cameras.Add(camera);
        _video.Diagnostics = new LiveDiagnostics(
            "connected", "connected", "host udp -> host udp", 123456, 90, 0,
            "video/H264 profile-level-id=42e01f", 1280, 720, "paused, readyState 0, 0x0", "NotAllowedError: play() was refused");
        var page = Render<LiveView>(parameters => parameters.Add(view => view.CameraId, camera.Id));
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".overlay")));

        // act
        page.Find("button.diagnostics-toggle").Click();

        // assert
        var text = page.Find(".diagnostics dl").TextContent;
        Assert.Contains("video/H264 profile-level-id=42e01f", text, StringComparison.Ordinal);
        Assert.Contains("90 / 0", text, StringComparison.Ordinal);
        Assert.Contains("NotAllowedError", text, StringComparison.Ordinal);
        Assert.Equal("Esconder diagnóstico", page.Find("button.diagnostics-toggle").TextContent.Trim());
        page.Find("button.diagnostics-toggle").Click();
        Assert.Empty(page.FindAll(".diagnostics dl"));
    }
}
