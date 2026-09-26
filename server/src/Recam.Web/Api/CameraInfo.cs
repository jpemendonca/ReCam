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
    bool TorchOn = false);
