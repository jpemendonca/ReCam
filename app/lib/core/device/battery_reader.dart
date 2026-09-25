import 'package:battery_plus/battery_plus.dart';

class BatteryReading {
  const BatteryReading({required this.level, required this.isCharging});

  final int level;
  final bool isCharging;

  @override
  bool operator ==(Object other) =>
      other is BatteryReading &&
      other.level == level &&
      other.isCharging == isCharging;

  @override
  int get hashCode => Object.hash(level, isCharging);
}

abstract interface class BatteryReader {
  Future<BatteryReading> read();
}

class PluginBatteryReader implements BatteryReader {
  final _battery = Battery();

  @override
  Future<BatteryReading> read() async {
    final level = await _battery.batteryLevel;
    final state = await _battery.batteryState;
    return BatteryReading(
      level: level.clamp(0, 100),
      isCharging: state == BatteryState.charging || state == BatteryState.full,
    );
  }
}
