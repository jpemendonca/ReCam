using Recam.Server.Domain;
using static Recam.Server.Tests.Support.ApiJson;

namespace Recam.Server.Tests.Domain;

public sealed class DeviceTorchTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    private static Device Paired(DeviceRole role) =>
        Device.Pair(PairingToken.Issue(role, Now).Token, "Test", AnyRole, Now).Value.Device;

    [Fact(DisplayName = "A publishing camera accepts a torch command")]
    public void AcceptTorchCommand_WhilePublishing_Succeeds()
    {
        // arrange
        var camera = Paired(DeviceRole.Camera);

        // act
        var result = camera.AcceptTorchCommand(publishing: true);

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "A camera that is not sending video has no torch to switch")]
    public void AcceptTorchCommand_WhileNotPublishing_ReturnsCameraNotPublishing()
    {
        // arrange
        var camera = Paired(DeviceRole.Camera);

        // act
        var result = camera.AcceptTorchCommand(publishing: false);

        // assert
        Assert.Equal(MediaErrors.CameraNotPublishing, result.Error);
    }

    [Fact(DisplayName = "A device that is not a camera is treated as an unknown camera")]
    public void AcceptTorchCommand_OnViewer_ReturnsCameraNotFound()
    {
        // arrange
        var viewer = Paired(DeviceRole.Viewer);

        // act
        var result = viewer.AcceptTorchCommand(publishing: true);

        // assert
        Assert.Equal(MediaErrors.CameraNotFound, result.Error);
    }
}
