using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Pages;
using Recam.Web.Pairing;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Pages;

public sealed class AddMonitorTests : BunitContext
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };
    private readonly FakeClipboard _clipboard = new();

    public AddMonitorTests()
    {
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddSingleton<IClipboard>(_clipboard);
        Services.AddTransient<AddDeviceController>();
        Services.AddTransient<InviteBrowserController>();
    }

    [Fact(DisplayName = "Add Monitor asks where it will watch before showing any QR code")]
    public void Render_AsksWhere()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");

        // act
        var page = Render<AddMonitor>();

        // assert
        Assert.Equal("Onde vai assistir?", page.Find("h2").TextContent);
        Assert.Equal(["Em outro celular com o app", "Em um navegador"], page.FindAll("button.option").Select(option => option.TextContent));
        Assert.Empty(page.FindAll(".qr"));
        Assert.Empty(_api.Tokens);
        Assert.Empty(_api.Invites);
    }

    [Fact(DisplayName = "Once a browser uses the invitation, the page goes back to Devices by itself")]
    public void InBrowser_WhenUsed_GoesBackToDevices()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var page = Render<AddMonitor>();
        page.FindAll("button.option")[1].Click();
        page.WaitForAssertion(() => Assert.NotNull(page.Find(".qr svg")));

        // act
        _api.UsedInvites.Add(_api.Invites[0].Id);

        // assert
        var navigation = Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(
            () => Assert.EndsWith("/devices?connected=browser", navigation.Uri, StringComparison.Ordinal), TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "In a browser shows the invitation link as a QR code, copies it and names the address")]
    public void InBrowser_ShowsInviteQrAndLink()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var page = Render<AddMonitor>();

        // act
        page.FindAll("button.option")[1].Click();
        page.WaitForAssertion(() => Assert.NotNull(page.Find(".qr svg")));
        page.Find("button.copy").Click();

        // assert
        var invite = Assert.Single(_api.Invites);
        Assert.Equal(invite.Url, page.Find("code.uri").TextContent);
        Assert.Equal("Vale por 10:00, uma vez só.", page.Find(".expires").TextContent);
        Assert.Equal([invite.Url], _clipboard.Written);
        Assert.Equal("Link copiado.", page.Find(".ok").TextContent);
        Assert.Contains("https://cameras.example.com", page.Find(".address").TextContent, StringComparison.Ordinal);
        Assert.Equal("true", page.FindAll("button.option")[1].GetAttribute("aria-pressed"));
        Assert.Empty(_api.Tokens);
    }

    [Fact(DisplayName = "On another phone shows the Monitor pairing QR, as before")]
    public void OnPhone_ShowsPairingQr()
    {
        // arrange
        using var _ = Culture.Use("en-US");
        var page = Render<AddMonitor>();

        // act
        page.FindAll("button.option")[0].Click();

        // assert
        page.WaitForAssertion(() => Assert.NotNull(page.Find(".qr svg")));
        Assert.Equal(DeviceKind.Monitor, Assert.Single(_api.Tokens).Kind);
        Assert.Empty(_api.Invites);
    }
}
