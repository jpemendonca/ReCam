import 'package:flutter/services.dart';
import 'package:permission_handler/permission_handler.dart' as permissions;

/// Android's battery optimization for this app. MIUI and One UI kill apps in the background
/// unless the person frees them, and a camera must keep running (SPECS.md 11).
abstract interface class BatteryOptimization {
  /// True when Android already lets the app run without battery restrictions.
  Future<bool> isIgnored();

  /// The phone maker, as Android reports it (e.g. "samsung", "Xiaomi").
  Future<String> manufacturer();

  /// Shows Android's own "stop optimizing battery usage" dialog.
  Future<void> requestIgnore();

  /// Opens this app's page in the system settings.
  Future<void> openAppSettings();
}

class PluginBatteryOptimization implements BatteryOptimization {
  static const _device = MethodChannel('io.recam.app/device');

  @override
  Future<bool> isIgnored() =>
      permissions.Permission.ignoreBatteryOptimizations.isGranted;

  @override
  Future<String> manufacturer() async {
    try {
      return await _device.invokeMethod<String>('manufacturer') ?? '';
    } on PlatformException {
      return '';
    } on MissingPluginException {
      return '';
    }
  }

  @override
  Future<void> requestIgnore() async {
    await permissions.Permission.ignoreBatteryOptimizations.request();
  }

  @override
  Future<void> openAppSettings() async {
    await permissions.openAppSettings();
  }
}
