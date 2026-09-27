using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Recam.Web.Api;
using Recam.Web.Devices;
using Recam.Web.Pages;
using Recam.Web.Pairing;
using Recam.Web.Realtime;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Devices;

public sealed class DevicesTests : BunitContext
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador · Chrome no Windows", "owner") };
    private readonly AddDeviceController _add;
    private readonly FakeDeviceHub _hub = new();

    public DevicesTests()
    {
        _add = new AddDeviceController(_api);
        Services.AddLocalization();
        Services.AddSingleton<IRecamApi>(_api);
        Services.AddSingleton<IDeviceHub>(_hub);
        Services.AddTransient<DevicesController>();
        Services.AddSingleton(_add);
    }

    [Fact(DisplayName = "The list has cameras and Monitors, and marks this browser, which cannot remove itself")]
    public void Render_MarksThisBrowser()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        _api.Cameras.Add(Support.Cameras.Make("Porta"));
        _api.OtherMonitors.Add(new DeviceInfo(Guid.NewGuid(), "Monitor", "viewer", false));

        // act
        var page = Render<DevicesPage>();

        // assert
        page.WaitForAssertion(() => Assert.Equal(["Câmeras", "Monitores"], page.FindAll("h2").Select(title => title.TextContent)));
        var monitors = page.FindAll("ul.devices")[1].QuerySelectorAll("li");
        Assert.Equal(2, monitors.Length);
        Assert.Equal("Este navegador", monitors[0].QuerySelector(".self")!.TextContent);
        Assert.Null(monitors[0].QuerySelector("button"));
        Assert.NotNull(monitors[1].QuerySelector("button.remove"));
    }

    [Fact(DisplayName = "When the server says the list changed, a new Monitor shows up without reopening the page")]
    public void DevicesChanged_NewMonitor_ShowsUp()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var page = Render<DevicesPage>();
        page.WaitForAssertion(() => Assert.Single(page.FindAll("ul.devices")[1].QuerySelectorAll("li")));
        var seen = new DateTimeOffset(2026, 9, 27, 13, 5, 0, TimeSpan.Zero);
        _api.OtherMonitors.Add(new DeviceInfo(Guid.NewGuid(), "Redmi 6A", "viewer", false, seen));

        // act
        _hub.SendDevicesChanged();

        // assert
        page.WaitForAssertion(() => Assert.Equal(2, page.FindAll("ul.devices")[1].QuerySelectorAll("li").Length));
        var state = page.FindAll("ul.devices")[1].QuerySelectorAll("li")[1].QuerySelector(".state")!.TextContent;
        Assert.StartsWith("Offline · visto por último em ", state, StringComparison.Ordinal);
        Assert.Contains(seen.ToLocalTime().ToString("g", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")), state, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "Removing asks first, then takes the device off the server")]
    public void Remove_Confirmed_RemovesCamera()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var camera = Support.Cameras.Make("Porta");
        _api.Cameras.Add(camera);
        var page = Render<DevicesPage>();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("button.remove")));
        page.Find("button.remove").Click();
        var question = page.Find(".confirm strong").TextContent;

        // act
        page.Find(".confirm button.danger").Click();

        // assert
        Assert.Equal("Remover Porta?", question);
        page.WaitForAssertion(() => Assert.Empty(page.FindAll(".confirm")));
        Assert.Equal([camera.Id], _api.Removed);
        Assert.Empty(page.FindAll("ul.devices")[0].QuerySelectorAll("li"));
    }

    [Fact(DisplayName = "Cancelling keeps the device")]
    public void Remove_Cancelled_KeepsDevice()
    {
        // arrange
        _api.Cameras.Add(Support.Cameras.Make("Porta"));
        var page = Render<DevicesPage>();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("button.remove")));
        page.Find("button.remove").Click();

        // act
        page.Find(".confirm button.secondary").Click();

        // assert
        Assert.Empty(page.FindAll(".confirm"));
        Assert.Empty(_api.Removed);
    }

    [Fact(DisplayName = "Add Monitor shows a Monitor QR, and goes to the devices when a phone pairs")]
    public async Task AddMonitor_Paired_GoesToDevices()
    {
        // arrange
        using var _ = Culture.Use("pt-BR");
        var page = Render<AddMonitor>();
        page.WaitForAssertion(() => Assert.NotNull(page.Find(".qr svg")));
        var steps = page.FindAll("ol.steps li").Select(step => step.TextContent).ToList();
        _api.UseLastToken();

        // act
        await page.InvokeAsync(_add.CheckPairedAsync);

        // assert
        Assert.Equal(DeviceKind.Monitor, _api.Tokens.Single().Kind);
        Assert.Equal("No celular que vai assistir, instale o app ReCam.", steps[0]);
        Assert.Equal("Leia este QR code. O celular vira um Monitor e vê as câmeras.", steps[2]);
        var navigation = Services.GetRequiredService<NavigationManager>();
        page.WaitForAssertion(() => Assert.EndsWith("/devices", navigation.Uri, StringComparison.Ordinal));
    }
}
