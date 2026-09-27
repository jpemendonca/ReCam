using Recam.Server.Domain;
using static Recam.Server.Tests.Support.ApiJson;

namespace Recam.Server.Tests.Domain;

public sealed class DeviceRecordingTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    private static Device Paired(DeviceRole role) =>
        Device.Pair(PairingToken.Issue(role, Now).Token, "Test", AnyRole, Now).Value.Device;

    [Theory(DisplayName = "A Monitor turns recording on and off for a camera")]
    [InlineData(DeviceRole.Owner)]
    [InlineData(DeviceRole.Viewer)]
    public void SetRecording_ByMonitor_ChangesTheCamera(DeviceRole monitorRole)
    {
        // arrange
        var monitor = Paired(monitorRole);
        var camera = Paired(DeviceRole.Camera);

        // act
        var on = camera.SetRecording(monitor, enabled: true);
        var recordingAfterOn = camera.RecordingEnabled;
        var off = camera.SetRecording(monitor, enabled: false);

        // assert
        Assert.True(on.IsSuccess);
        Assert.True(recordingAfterOn);
        Assert.True(off.IsSuccess);
        Assert.False(camera.RecordingEnabled);
    }

    [Fact(DisplayName = "A camera cannot change recording, not even its own")]
    public void SetRecording_ByCamera_ReturnsNotAMonitor()
    {
        // arrange
        var camera = Paired(DeviceRole.Camera);

        // act
        var result = camera.SetRecording(camera, enabled: false);

        // assert
        Assert.Equal(DeviceErrors.NotAMonitor, result.Error);
        Assert.True(camera.RecordingEnabled);
    }

    [Theory(DisplayName = "A new camera records from the start; Monitors have nothing to record")]
    [InlineData(DeviceRole.Camera, true)]
    [InlineData(DeviceRole.Viewer, false)]
    [InlineData(DeviceRole.Owner, false)]
    public void Pair_NewDevice_RecordsWhenCamera(DeviceRole role, bool recording)
    {
        // act
        var device = Paired(role);

        // assert
        Assert.Equal(recording, device.RecordingEnabled);
    }

    [Fact(DisplayName = "Only cameras record")]
    public void SetRecording_OnMonitor_ReturnsCameraNotFound()
    {
        // arrange
        var owner = Paired(DeviceRole.Owner);
        var viewer = Paired(DeviceRole.Viewer);

        // act
        var result = viewer.SetRecording(owner, enabled: true);

        // assert
        Assert.Equal(MediaErrors.CameraNotFound, result.Error);
    }

    [Fact(DisplayName = "A camera that sends VP8 cannot record")]
    public void SetRecording_ForCameraWithoutH264_ReturnsRecordingNeedsH264()
    {
        // arrange
        var owner = Paired(DeviceRole.Owner);
        var camera = Paired(DeviceRole.Camera);
        camera.ReportVideoCodecs(supportsH264: false);

        // act
        var result = camera.SetRecording(owner, enabled: true);

        // assert
        Assert.Equal(MediaErrors.RecordingNeedsH264, result.Error);
        Assert.False(camera.CanRecord);
    }

    [Fact(DisplayName = "A recording camera that turns out to lack H.264 stops recording")]
    public void ReportVideoCodecs_WithoutH264_TurnsRecordingOff()
    {
        // arrange
        var owner = Paired(DeviceRole.Owner);
        var camera = Paired(DeviceRole.Camera);
        camera.SetRecording(owner, enabled: true);

        // act
        camera.ReportVideoCodecs(supportsH264: false);

        // assert
        Assert.False(camera.RecordingEnabled);
    }
}
