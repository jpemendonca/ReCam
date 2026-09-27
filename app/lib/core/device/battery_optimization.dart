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

  /// True when the person restricted the app's background use (Android 9+).
  Future<bool> isBackgroundRestricted();

  /// False when the app's notifications are off, which hides the camera's ongoing notice.
  Future<bool> areNotificationsEnabled();

  /// Asks for notifications (Android 13+), or opens the app's settings where it cannot ask.
  Future<void> requestNotifications();
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

  @override
  Future<bool> isBackgroundRestricted() async =>
      await _ask('isBackgroundRestricted') ?? false;

  @override
  Future<bool> areNotificationsEnabled() async =>
      await _ask('areNotificationsEnabled') ?? true;

  @override
  Future<void> requestNotifications() async {
    final status = await permissions.Permission.notification.request();
    if (!status.isGranted) await permissions.openAppSettings();
  }

  // A check the phone cannot answer counts as fine: these are recommendations.
  static Future<bool?> _ask(String method) async {
    try {
      return await _device.invokeMethod<bool>(method);
    } on PlatformException {
      return null;
    } on MissingPluginException {
      return null;
    }
  }
}
