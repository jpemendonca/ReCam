using Recam.Server.Domain;
using static Recam.Server.Tests.Support.ApiJson;

namespace Recam.Server.Tests.Domain;

public sealed class DeviceTelemetryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);

    private static Device Paired(DeviceRole role) =>
        Device.Pair(PairingToken.Issue(role, Now).Token, "Test", AnyRole, Now).Value.Device;

    [Fact(DisplayName = "A camera stores its battery report")]
    public void ReportTelemetry_FromCamera_StoresValues()
    {
        // arrange
        var camera = Paired(DeviceRole.Camera);

        // act
        var result = camera.ReportTelemetry(80, isCharging: true, Now.AddMinutes(1));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal(80, camera.BatteryLevel);
        Assert.True(camera.IsCharging);
        Assert.Equal(Now.AddMinutes(1), camera.TelemetryAt);
    }

    [Fact(DisplayName = "A battery level outside 0 to 100 is refused")]
    public void ReportTelemetry_WithLevelAbove100_ReturnsInvalidBatteryLevel()
    {
        // arrange
        var camera = Paired(DeviceRole.Camera);

        // act
        var result = camera.ReportTelemetry(101, isCharging: false, Now);

        // assert
        Assert.Equal(DeviceErrors.InvalidBatteryLevel, result.Error);
        Assert.Null(camera.BatteryLevel);
    }

    [Fact(DisplayName = "A viewer cannot report telemetry")]
    public void ReportTelemetry_FromViewer_ReturnsNotACamera()
    {
        // arrange
        var viewer = Paired(DeviceRole.Viewer);

        // act
        var result = viewer.ReportTelemetry(50, isCharging: false, Now);

        // assert
        Assert.Equal(DeviceErrors.NotACamera, result.Error);
    }
}
