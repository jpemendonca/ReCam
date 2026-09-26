using Recam.Web.Api;
using Recam.Web.Pairing;
using Recam.Web.Tests.Support;

namespace Recam.Web.Tests.Pairing;

public sealed class AddDeviceControllerTests
{
    private readonly FakeRecamApi _api = new() { Me = new MeInfo(Guid.NewGuid(), "Navegador", "owner") };

    [Fact(DisplayName = "Starting shows a camera QR with its time left")]
    public async Task Start_Camera_ShowsQr()
    {
        // arrange
        var controller = new AddDeviceController(_api);

        // act
        await controller.StartAsync(DeviceKind.Camera);

        // assert
        Assert.Equal(AddDeviceState.Ready, controller.State);
        Assert.Equal(DeviceKind.Camera, _api.Tokens.Single().Kind);
        Assert.Equal(_api.Tokens.Single().Token.QrUri, controller.QrUri);
        Assert.StartsWith("<svg", controller.QrSvg, StringComparison.Ordinal);
        Assert.Equal(TimeSpan.FromMinutes(10), controller.Remaining);
    }

    [Fact(DisplayName = "The countdown runs, and an expired QR is replaced by a new one")]
    public async Task Tick_PastExpiry_CreatesNewQr()
    {
        // arrange
        _api.TokenValidFor = TimeSpan.FromSeconds(2);
        var controller = new AddDeviceController(_api);
        await controller.StartAsync(DeviceKind.Camera);
        await controller.Tick(TimeSpan.FromSeconds(1));
        var afterOneSecond = controller.Remaining;

        // act
        await controller.Tick(TimeSpan.FromSeconds(1));

        // assert
        Assert.Equal(TimeSpan.FromSeconds(1), afterOneSecond);
        Assert.Equal(2, _api.Tokens.Count);
        Assert.Equal(_api.Tokens[1].Token.QrUri, controller.QrUri);
    }

    [Fact(DisplayName = "When a phone uses the QR, the controller says it paired")]
    public async Task CheckPaired_TokenUsed_Paired()
    {
        // arrange
        var controller = new AddDeviceController(_api);
        await controller.StartAsync(DeviceKind.Camera);
        await controller.CheckPairedAsync();
        var beforeScan = controller.State;
        _api.UseLastToken();

        // act
        await controller.CheckPairedAsync();

        // assert
        Assert.Equal(AddDeviceState.Ready, beforeScan);
        Assert.Equal(AddDeviceState.Paired, controller.State);
    }

    [Fact(DisplayName = "Without the server the QR fails, and retrying shows it")]
    public async Task Start_Offline_FailsThenRetries()
    {
        // arrange
        _api.Offline = true;
        var controller = new AddDeviceController(_api);
        await controller.StartAsync(DeviceKind.Camera);
        var failed = controller.State;
        _api.Offline = false;

        // act
        await controller.StartAsync(DeviceKind.Camera);

        // assert
        Assert.Equal(AddDeviceState.Failed, failed);
        Assert.Equal(AddDeviceState.Ready, controller.State);
    }
}
