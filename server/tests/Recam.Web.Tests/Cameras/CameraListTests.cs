using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Cameras;
using Recam.Web.Live;
using Recam.Web.Realtime;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Cameras;

public sealed class CameraListTests : BunitContext
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly FakeDeviceHub _hub = new();
    private readonly FakeLiveVideo _video = new();

    public CameraListTests()
    {
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddSingleton<IDeviceHub>(_hub);
        Services.AddSingleton<CameraListController>();
        Services.AddSingleton(_video);
        Services.AddTransient<ILiveVideo>(provider => provider.GetRequiredService<FakeLiveVideo>());
        Services.AddSingleton<SoundChoice>();
        Services.AddTransient<LiveController>();
    }

    [Fact(DisplayName = "Each camera shows its state, battery, temperature and a link to its live view")]
    public void Render_Cameras_ShowsStatus()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta", publishing: true, recording: true);
        _api.Cameras.Add(camera);

        // act
        var list = Render<CameraList>();

        // assert
        Assert.Equal("Porta", list.Find(".name").TextContent);
        Assert.Equal("Online · transmitindo · Bateria 80% · 32 °C", list.Find(".status").TextContent);
        Assert.Equal("Gravando", list.Find(".badge").TextContent);
        Assert.Contains("live", list.Find(".dot").ClassList);
        Assert.Equal($"cameras/{camera.Id}", list.Find("a.open").GetAttribute("href"));
        Assert.Equal("Conectado", list.Find(".hub").TextContent.Trim());
    }

    [Fact(DisplayName = "A camera without H.264 cannot record, and says why")]
    public void Render_NoH264_ExplainsInsteadOfToggle()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        _api.Cameras.Add(Support.Cameras.Make("Sala", canRecord: false));

        // act
        var list = Render<CameraList>();

        // assert
        Assert.Empty(list.FindAll("input[type=checkbox]"));
        Assert.Equal("Não grava: este celular não tem H.264", list.Find(".muted").TextContent);
    }

    [Fact(DisplayName = "Turning Record always on asks the server for that camera")]
    public void Toggle_RecordAlways_CallsHub()
    {
        // arrange
        var camera = Support.Cameras.Make("Porta");
        _api.Cameras.Add(camera);
        var list = Render<CameraList>();

        // act
        list.Find("input[type=checkbox]").Change(true);

        // assert
        Assert.Contains($"SetRecording {camera.Id} True", _hub.Calls);
    }

    [Fact(DisplayName = "An empty server says there is no camera yet")]
    public void Render_NoCameras_SaysSo()
    {
        // arrange
        using var _ = Culture.Use("en-US");

        // act
        var list = Render<CameraList>();

        // assert
        Assert.Equal("No cameras yet.", list.Find(".note").TextContent);
    }

    [Fact(DisplayName = "The dot is green while the camera sends a picture and red otherwise")]
    public void Render_Dot_GreenOnlyWhileSending()
    {
        // arrange
        _api.Cameras.Add(Support.Cameras.Make("Porta", publishing: true));
        _api.Cameras.Add(Support.Cameras.Make("Sala"));

        // act
        var list = Render<CameraList>();

        // assert
        Assert.Equal(["dot live", "dot off"], list.FindAll(".dot").Select(dot => dot.ClassName));
    }

    [Fact(DisplayName = "Clicking anywhere on the card opens the live video")]
    public void Click_Card_OpensLiveView()
    {
        // arrange
        var camera = Support.Cameras.Make("Porta");
        _api.Cameras.Add(camera);
        var list = Render<CameraList>();

        // act
        list.Find("li.camera").Click();

        // assert
        var navigation = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith($"/cameras/{camera.Id}", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Show picture opens a live preview in the card, which closes by itself and stops watching")]
    public void Preview_Open_ClosesByItself()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta");
        _api.Cameras.Add(camera);
        var list = Render<CameraList>(parameters => parameters.Add(cameras => cameras.PreviewCloseAfter, TimeSpan.FromMilliseconds(200)));
        Assert.Empty(list.FindAll(".preview"));

        // act
        list.Find("button.preview-toggle").Click();

        // assert
        list.WaitForAssertion(() => Assert.Contains($"WatchCamera {camera.Id}", _hub.Calls));
        Assert.Equal("Esconder imagem", list.Find("button.preview-toggle").TextContent.Trim());
        list.WaitForAssertion(() => Assert.Empty(list.FindAll(".preview")), TimeSpan.FromSeconds(5));
        list.WaitForAssertion(() => Assert.Contains($"UnwatchCamera {camera.Id}", _hub.Calls));
        Assert.Equal("Ver imagem", list.Find("button.preview-toggle").TextContent.Trim());
    }

    [Fact(DisplayName = "A card shows the flashlight while it is on, and follows the camera's reports")]
    public void Render_TorchOn_ShowsIconUntilOff()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta", publishing: true, torchOn: true);
        _api.Cameras.Add(camera);
        var list = Render<CameraList>();
        var shown = list.Find(".torch").GetAttribute("title");

        // act
        _hub.SendTorch(camera.Id, false);

        // assert
        Assert.Equal("Lanterna ligada", shown);
        list.WaitForAssertion(() => Assert.Empty(list.FindAll(".torch")));
    }
}
