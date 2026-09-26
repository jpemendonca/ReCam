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
        page.Find(".controls button").Click();

        // assert
        Assert.Equal("Porta", page.Find("h1").TextContent);
        Assert.Contains($"SetTorch {camera.Id} True", _hub.Calls);
        Assert.Equal("Ligar lanterna", page.Find(".controls button").TextContent.Trim());
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
        page.WaitForAssertion(() => Assert.Equal("Turn flashlight off", page.Find(".controls button").TextContent.Trim()));
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
        page.Find(".controls button").Click();

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
        Assert.Equal("Desligar lanterna", page.Find(".controls button").TextContent.Trim());
    }
}
