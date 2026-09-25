import '../core/device/battery_optimization.dart';

/// Which steps free the camera from battery restrictions on this phone.
enum BatteryGuide { samsung, xiaomi, generic }

/// Decides whether camera mode needs the battery guide first, and which one.
class BatteryGuideController {
  BatteryGuideController({required this.optimization});

  final BatteryOptimization optimization;

  /// Null when the phone already lets the app run freely.
  Future<BatteryGuide?> check() async {
    if (await optimization.isIgnored()) return null;
    return forManufacturer(await optimization.manufacturer());
  }

  /// Redmi and POCO phones run MIUI/HyperOS too, and some report their brand as maker.
  static BatteryGuide forManufacturer(String manufacturer) {
    final maker = manufacturer.trim().toLowerCase();
    if (maker == 'samsung') return BatteryGuide.samsung;
    if (const {'xiaomi', 'redmi', 'poco'}.contains(maker)) {
      return BatteryGuide.xiaomi;
    }
    return BatteryGuide.generic;
  }
}
