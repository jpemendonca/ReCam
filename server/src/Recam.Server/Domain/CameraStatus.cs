namespace Recam.Server.Domain;

/// <summary>What a viewer knows about a camera: stored telemetry plus live presence.</summary>
public sealed record CameraStatus(
    Guid Id,
    string Name,
    bool Online,
    bool Publishing,
    int? BatteryLevel,
    bool? IsCharging,
    DateTimeOffset? TelemetryAt);
