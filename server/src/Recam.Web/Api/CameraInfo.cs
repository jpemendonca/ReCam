namespace Recam.Web.Api;

/// <summary>A camera as the server reports it (GET /api/cameras and CameraStatusChanged).</summary>
public sealed record CameraInfo(
    Guid Id,
    string Name,
    bool Online,
    bool Publishing,
    int? BatteryLevel,
    bool? IsCharging,
    double? TemperatureC,
    DateTimeOffset? TelemetryAt,
    bool Recording,
    bool CanRecord,
    bool TorchOn = false,
    CameraRecordingState RecordingState = CameraRecordingState.Off)
{
    /// <summary>
    /// The state to show next to the switch. Right after a switch, before the server looks at the
    /// disk again, it shows what was asked: "Starting" when turned on, "Off" when turned off.
    /// </summary>
    public CameraRecordingState ShownRecordingState => (Recording, RecordingState) switch
    {
        (true, CameraRecordingState.Off) => CameraRecordingState.Starting,
        (false, not CameraRecordingState.NeedsH264) => CameraRecordingState.Off,
        _ => RecordingState,
    };
}
