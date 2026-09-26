using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Cameras;
using Recam.Web.Pages;
using Recam.Web.Pairing;
using Recam.Web.Realtime;
using Recam.Web.Start;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Pages;

public sealed class HomeTests : BunitContext
{
    private readonly FakeRecamApi _api = new();

    public HomeTests()
    {
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddTransient<StartController>();
        Services.AddSingleton<IDeviceHub>(new FakeDeviceHub());
        Services.AddSingleton<CameraListController>();
        Services.AddTransient<AddDeviceController>();
    }

    [Theory(DisplayName = "Without a Monitor, the start page asks for the first-time code, in the browser's language")]
    [InlineData("en-US", "First time here", "Remember on this computer")]
    [InlineData("pt-BR", "Primeiro acesso", "Lembrar neste computador")]
    public void Render_NoMonitor_ShowsCodeForm(string language, string title, string remember)
    {
        // arrange
        using var _ = Culture.Use(language);

        // act
        var page = Render<Home>();

        // assert
        Assert.Equal(title, page.Find("h1").TextContent);
        Assert.Contains(remember, page.Find("label.check").TextContent, StringComparison.Ordinal);
        Assert.True(page.Find("label.check input").HasAttribute("checked"));
    }

    [Fact(DisplayName = "Typing the right code opens the cameras, with Add camera")]
    public void Submit_RightCode_ShowsMonitor()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var page = Render<Home>();
        page.Find("#code").Change(FakeRecamApi.Code);

        // act
        page.Find("form").Submit();

        // assert
        page.WaitForAssertion(() => Assert.Equal("Câmeras", page.Find("h1").TextContent));
        Assert.Equal("Adicionar câmera", page.Find(".head a.button").TextContent);
    }

    [Fact(DisplayName = "A wrong code shows the reason under the field")]
    public void Submit_WrongCode_ShowsError()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var page = Render<Home>();
        page.Find("#code").Change("ZZZZ-ZZZZ");

        // act
        page.Find("form").Submit();

        // assert
        page.WaitForAssertion(() => Assert.StartsWith("Código errado.", page.Find(".error").TextContent, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "When another device is the Monitor, the page points to Connect browser")]
    public void Render_OtherMonitor_ShowsServerTaken()
    {
        // arrange
        using var _ = Culture.Use("en-US");
        _api.OtherMonitor = true;

        // act
        var page = Render<Home>();

        // assert
        Assert.Equal("This server already has a Monitor", page.Find("h1").TextContent);
        Assert.Contains("Connect browser", page.Find("p").TextContent, StringComparison.Ordinal);
    }
}
