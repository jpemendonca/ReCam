import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/device/battery_reader.dart';
import '../core/device/keep_alive.dart';
import '../core/device/screen_controller.dart';
import '../core/network/hub_session.dart';
import '../core/storage/credential_store.dart';

typedef CameraModeFactory = CameraModeController Function(
  PairedSession session,
);

/// Camera mode while idle: stays connected to the hub and reports the battery. Sends a
/// report when the connection opens, when the reading changes and at least every
/// [reportInterval].
class CameraModeController extends ChangeNotifier {
  CameraModeController({
    required this._hub,
    required this._battery,
    required this._screen,
    required this._keepAlive,
    DateTime Function()? now,
    this.reportInterval = const Duration(seconds: 60),
    this.checkInterval = const Duration(seconds: 15),
  }) : _now = now ?? DateTime.now;

  final HubSession _hub;
  final BatteryReader _battery;
  final ScreenController _screen;
  final KeepAlive _keepAlive;
  final DateTime Function() _now;
  final Duration reportInterval;
  final Duration checkInterval;

  Timer? _checkTimer;
  BatteryReading? _lastSent;
  DateTime? _lastSentAt;

  bool get connected => _hub.connected;

  BatteryReading? get lastReading => _lastSent;

  Future<void> start({
    required String notificationTitle,
    required String notificationText,
  }) async {
    _hub.addListener(notifyListeners);
    _hub.onConnected = () => unawaited(_report(force: true));
    await _keepAlive.start(title: notificationTitle, text: notificationText);
    await _screen.enterCameraMode();
    _hub.start();
    _checkTimer = Timer.periodic(checkInterval, (_) => checkBattery());
  }

  /// Sends the battery if it changed or the last report is older than [reportInterval].
  Future<void> checkBattery() => _report(force: false);

  Future<void> stop() async {
    _checkTimer?.cancel();
    _checkTimer = null;
    _hub.removeListener(notifyListeners);
    await _hub.stop();
    await _screen.exitCameraMode();
    await _keepAlive.stop();
  }

  Future<void> _report({required bool force}) async {
    if (!_hub.connected) return;
    final reading = await _battery.read();
    final lastSentAt = _lastSentAt;
    final due =
        lastSentAt == null || _now().difference(lastSentAt) >= reportInterval;
    if (!force && reading == _lastSent && !due) return;
    await _hub.client.invoke('ReportTelemetry', [
      reading.level,
      reading.isCharging,
    ]);
    _lastSent = reading;
    _lastSentAt = _now();
    notifyListeners();
  }

  @override
  void dispose() {
    _checkTimer?.cancel();
    _hub.removeListener(notifyListeners);
    super.dispose();
  }
}
