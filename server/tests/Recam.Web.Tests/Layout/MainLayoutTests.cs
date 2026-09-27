using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Cameras;
using Recam.Web.Layout;
using Recam.Web.Realtime;
using Recam.Web.Start;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Layout;

public sealed class MainLayoutTests : BunitContext
{
    private readonly FakeRecamApi _api = new();
    private readonly FakeDeviceHub _hub = new();

    public MainLayoutTests()
    {
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddSingleton<StartController>();
        Services.AddSingleton<IDeviceHub>(_hub);
        Services.AddSingleton<CameraListController>();
    }

    [Fact(DisplayName = "A Monitor sees the navigation and Sign out in the top bar")]
    public void Render_Monitor_ShowsNavigation()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        _api.Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner");

        // act
        var layout = Render<MainLayout>();

        // assert
        layout.WaitForAssertion(() => Assert.Equal(["Câmeras", "Gravações", "Aparelhos", "Configurações"], layout.FindAll("nav a").Select(link => link.TextContent)));
        Assert.Equal("Sair", layout.Find("nav button").TextContent);
        Assert.True(_hub.Connected);
    }

    [Fact(DisplayName = "On a phone the menu button opens the sections, and choosing one closes them")]
    public void MenuButton_OpensAndClosesOnNavigation()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        _api.Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner");
        var layout = Render<MainLayout>();
        layout.WaitForAssertion(() => Assert.NotEmpty(layout.FindAll("button.menu-toggle")));
        var closed = layout.Find("nav").ClassList.Contains("open");

        // act
        layout.Find("button.menu-toggle").Click();
        var opened = (layout.Find("nav").ClassList.Contains("open"), layout.Find("button.menu-toggle").GetAttribute("aria-expanded"));
        Services.GetRequiredService<NavigationManager>().NavigateTo("devices");

        // assert
        Assert.False(closed);
        Assert.Equal((true, "true"), opened);
        layout.WaitForAssertion(() => Assert.False(layout.Find("nav").ClassList.Contains("open")));
        Assert.Equal("Menu", layout.Find("button.menu-toggle").GetAttribute("aria-label"));
    }

    [Fact(DisplayName = "Before the code, the top bar has only the name")]
    public void Render_NotMonitor_NoNavigation()
    {
        // act
        var layout = Render<MainLayout>();

        // assert
        layout.WaitForAssertion(() => Assert.Equal(StartState.NeedsCode, Services.GetRequiredService<StartController>().State));
        Assert.Empty(layout.FindAll("nav"));
        Assert.False(_hub.Connected);
    }

    [Fact(DisplayName = "Sign out revokes this browser and starts the app over")]
    public void SignOut_RevokesAndReloads()
    {
        // arrange
        _api.Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner");
        var layout = Render<MainLayout>();
        layout.WaitForAssertion(() => Assert.NotEmpty(layout.FindAll("nav button")));

        // act
        layout.Find("nav button").Click();

        // assert
        layout.WaitForAssertion(() => Assert.Null(_api.Me));
        var navigation = Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();
        Assert.True(navigation.History.First().Options.ForceLoad);
    }
}
