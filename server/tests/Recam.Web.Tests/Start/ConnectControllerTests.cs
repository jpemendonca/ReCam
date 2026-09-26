using Recam.Web.Api;
using Recam.Web.Start;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Start;

public sealed class ConnectControllerTests
{
    private readonly FakeRecamApi _api = new() { OtherMonitor = true };

    [Fact(DisplayName = "The browser shows a QR and waits until a Monitor phone approves")]
    public async Task Check_WaitsThenConnects()
    {
        // arrange
        var controller = new ConnectController(_api);
        await controller.StartAsync();
        await controller.CheckAsync();
        var beforeApproval = controller.Connected;
        _api.ApproveLastLink();

        // act
        await controller.CheckAsync();

        // assert
        Assert.StartsWith("<svg", controller.QrSvg, StringComparison.Ordinal);
        Assert.False(beforeApproval);
        Assert.True(controller.Connected);
    }

    [Fact(DisplayName = "An expired QR is replaced by a new one")]
    public async Task Tick_PastExpiry_NewLink()
    {
        // arrange
        _api.TokenValidFor = TimeSpan.FromSeconds(1);
        var controller = new ConnectController(_api);
        await controller.StartAsync();

        // act
        await controller.Tick(TimeSpan.FromSeconds(1));

        // assert
        Assert.Equal(2, _api.Links.Count);
    }

    [Fact(DisplayName = "Without the server the QR fails, and retrying shows it")]
    public async Task Start_Offline_FailsThenRetries()
    {
        // arrange
        _api.Offline = true;
        var controller = new ConnectController(_api);
        await controller.StartAsync();
        var failed = controller.Failed;
        _api.Offline = false;

        // act
        await controller.StartAsync();

        // assert
        Assert.True(failed);
        Assert.False(controller.Failed);
        Assert.NotNull(controller.QrSvg);
    }
}
