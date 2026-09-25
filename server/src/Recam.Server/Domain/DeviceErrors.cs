namespace Recam.Server.Domain;

public static class DeviceErrors
{
    public static readonly DomainError NotACamera =
        new("device.not_a_camera", "Only camera devices report telemetry.", ErrorType.Forbidden);

    public static readonly DomainError InvalidBatteryLevel =
        new("device.invalid_battery_level", "Battery level must be between 0 and 100.", ErrorType.Validation);

    public static readonly DomainError NotFound =
        new("device.not_found", "There is no such device.", ErrorType.NotFound);

    public static readonly DomainError CannotRemoveItself =
        new("device.cannot_remove_itself", "A phone leaves by itself with Reset app, not from the device list.", ErrorType.Conflict);

    public static readonly DomainError NotAMonitor =
        new("device.not_a_monitor", "Only a Monitor can change this.", ErrorType.Forbidden);

    public static readonly DomainError InvalidTemperature =
        new("device.invalid_temperature", "Temperature must be between -40 and 120 °C.", ErrorType.Validation);
}
