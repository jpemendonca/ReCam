namespace Recam.Server.Features.Realtime;

/// <summary>
/// What a camera reports about itself. One object, not loose arguments, so a reading the phone
/// could not take travels as null.
/// </summary>
public sealed record TelemetryReport(int BatteryLevel, bool IsCharging, double? TemperatureC, bool? SupportsH264 = null);
