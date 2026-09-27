import '../core/device/battery_optimization.dart';

/// Which tips help on this phone, beyond what the app can check.
enum BatteryGuide { samsung, xiaomi, generic }

/// A setting the app reads by itself, so the person never has to tick anything.
enum BatteryCheck { optimization, background, notifications }

/// This phone's maker and which checked settings still hold the camera back.
class BatteryStatus {
  const BatteryStatus({required this.guide, required this.missing});

  final BatteryGuide guide;
  final Set<BatteryCheck> missing;

  bool get allGood => missing.isEmpty;
}

/// Reads the battery settings that keep the camera running. They are recommendations: the
/// camera works without them, but Android may close it in the background.
class BatteryGuideController {
  BatteryGuideController({required this.optimization});

  final BatteryOptimization optimization;

  Future<BatteryStatus> status() async {
    final missing = {
      if (!await optimization.isIgnored()) BatteryCheck.optimization,
      if (await optimization.isBackgroundRestricted()) BatteryCheck.background,
      if (!await optimization.areNotificationsEnabled())
        BatteryCheck.notifications,
    };
    return BatteryStatus(
      guide: forManufacturer(await optimization.manufacturer()),
      missing: missing,
    );
  }

  /// Opens the Android screen that fixes [check].
  Future<void> fix(BatteryCheck check) => switch (check) {
    BatteryCheck.optimization => optimization.requestIgnore(),
    BatteryCheck.background => optimization.openAppSettings(),
    BatteryCheck.notifications => optimization.requestNotifications(),
  };

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
