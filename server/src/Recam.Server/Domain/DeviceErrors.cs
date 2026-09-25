namespace Recam.Server.Domain;

public static class DeviceErrors
{
    public static readonly DomainError NotACamera =
        new("device.not_a_camera", "Only camera devices report telemetry.", ErrorType.Forbidden);

    public static readonly DomainError InvalidBatteryLevel =
        new("device.invalid_battery_level", "Battery level must be between 0 and 100.", ErrorType.Validation);

    public static readonly DomainError InvalidTemperature =
        new("device.invalid_temperature", "Temperature must be between -40 and 120 °C.", ErrorType.Validation);
}
