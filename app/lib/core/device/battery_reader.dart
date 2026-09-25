import 'package:battery_plus/battery_plus.dart';
import 'package:flutter/services.dart';

class BatteryReading {
  const BatteryReading({
    required this.level,
    required this.isCharging,
    this.temperatureC,
  });

  final int level;
  final bool isCharging;

  /// Battery temperature in °C; null when the phone does not tell.
  final double? temperatureC;

  // Whole degrees: tenths change all the time and would send a report every check.
  int? get _wholeDegrees => temperatureC?.round();

  @override
  bool operator ==(Object other) =>
      other is BatteryReading &&
      other.level == level &&
      other.isCharging == isCharging &&
      other._wholeDegrees == _wholeDegrees;

  @override
  int get hashCode => Object.hash(level, isCharging, _wholeDegrees);
}

abstract interface class BatteryReader {
  Future<BatteryReading> read();
}

class PluginBatteryReader implements BatteryReader {
  static const _device = MethodChannel('io.recam.app/device');

  final _battery = Battery();

  @override
  Future<BatteryReading> read() async {
    final level = await _battery.batteryLevel;
    final state = await _battery.batteryState;
    return BatteryReading(
      level: level.clamp(0, 100),
      isCharging: state == BatteryState.charging || state == BatteryState.full,
      temperatureC: await _temperature(),
    );
  }

  static Future<double?> _temperature() async {
    try {
      return await _device.invokeMethod<double>('batteryTemperature');
    } on PlatformException {
      return null;
    } on MissingPluginException {
      return null;
    }
  }
}
