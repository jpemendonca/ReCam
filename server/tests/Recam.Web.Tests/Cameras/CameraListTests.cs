using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Cameras;
using Recam.Web.Realtime;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Cameras;

public sealed class CameraListTests : BunitContext
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly FakeDeviceHub _hub = new();

    public CameraListTests()
    {
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddSingleton<IDeviceHub>(_hub);
        Services.AddSingleton<CameraListController>();
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
}
