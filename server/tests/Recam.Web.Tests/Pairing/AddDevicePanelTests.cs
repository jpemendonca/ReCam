using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Cameras;
using Recam.Web.Pages;
using Recam.Web.Pairing;
using Recam.Web.Realtime;
using Recam.Web.Start;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Pairing;

public sealed class AddDevicePanelTests : BunitContext
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly AddDeviceController _add;

    public AddDevicePanelTests()
    {
        _add = new AddDeviceController(_api);
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddSingleton<IDeviceHub>(new FakeDeviceHub());
        Services.AddSingleton<CameraListController>();
        Services.AddTransient<StartController>();
        Services.AddSingleton(_add);
    }

    [Fact(DisplayName = "With no camera yet, the start page walks through adding the first one, with the QR")]
    public void Home_NoCameras_ShowsGuide()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");

        // act
        var page = Render<Home>();

        // assert
        page.WaitForAssertion(() => Assert.Equal("Adicione sua primeira câmera", page.Find("h2").TextContent));
        var steps = page.FindAll("ol.steps li").Select(step => step.TextContent).ToList();
        Assert.Equal("Abra o app e toque em Ler QR code.", steps[1]);
        Assert.Equal("Pronto: o vídeo dela abre aqui.", steps[3]);
        Assert.NotNull(page.Find(".qr svg"));
        Assert.Equal("Expira em 10:00", page.Find(".expires").TextContent);
        Assert.Equal(_api.Tokens.Single().Token.QrUri, page.Find("code.uri").TextContent);
    }

    [Fact(DisplayName = "When the first camera pairs, the page asks for the recording space before its live video")]
    public async Task Home_FirstCameraPairs_AsksForSpace()
    {
        // arrange
        var page = Render<Home>();
        page.WaitForAssertion(() => Assert.NotNull(page.Find(".qr svg")));
        _api.UseLastToken("Porta");

        // act
        await page.InvokeAsync(_add.CheckPairedAsync);

        // assert
        var camera = Assert.Single(_api.Cameras);
        var navigation = Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(() => Assert.EndsWith($"/first-space/{camera.Id}", navigation.Uri, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Adding another camera goes back to the list, where it shows up")]
    public async Task AddCamera_Paired_BackToListWithCamera()
    {
        // arrange
        _api.Cameras.Add(Support.Cameras.Make("Porta"));
        var page = Render<AddCamera>();
        page.WaitForAssertion(() => Assert.NotNull(page.Find(".qr svg")));
        _api.UseLastToken("Garagem");

        // act
        await page.InvokeAsync(_add.CheckPairedAsync);

        // assert
        var navigation = Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(() => Assert.Equal(navigation.BaseUri, navigation.Uri));
        var cameras = Services.GetRequiredService<CameraListController>().Cameras;
        Assert.Equal(["Garagem", "Porta"], cameras.Select(camera => camera.Name));
        Assert.Equal(3, page.FindAll("ol.steps li").Count);
    }
}
