import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/device/battery_reader.dart';
import '../core/device/keep_alive.dart';
import '../core/device/screen_controller.dart';
import '../core/media/webrtc_publisher.dart';
import '../core/network/hub_session.dart';
import '../core/storage/credential_store.dart';

typedef CameraModeFactory = CameraModeController Function(
  PairedSession session,
);

/// Camera mode: stays connected to the hub, reports the battery, and publishes video only
/// while the server asks (someone is watching). Sends a battery report when the connection
/// opens, when the reading changes and at least every [reportInterval].
class CameraModeController extends ChangeNotifier {
  CameraModeController({
    required this._hub,
    required this._battery,
    required this._screen,
    required this._keepAlive,
    required this._publisher,
    DateTime Function()? now,
    this.reportInterval = const Duration(seconds: 60),
    this.checkInterval = const Duration(seconds: 15),
  }) : _now = now ?? DateTime.now;

  final HubSession _hub;
  final BatteryReader _battery;
  final ScreenController _screen;
  final KeepAlive _keepAlive;
  final WebRtcPublisher _publisher;
  final DateTime Function() _now;
  final Duration reportInterval;
  final Duration checkInterval;

  Timer? _checkTimer;
  BatteryReading? _lastSent;
  DateTime? _lastSentAt;
  bool _publishing = false;
  bool _torchOn = false;
  Future<void> _publishingChange = Future.value();

  bool get connected => _hub.connected;

  /// True when the server refused this camera's pairing; camera mode cannot go on.
  bool get pairingLost => _hub.rejected;

  bool get publishing => _publishing;

  bool get torchOn => _torchOn;

  BatteryReading? get lastReading => _lastSent;

  Future<void> start({
    required String notificationTitle,
    required String notificationText,
  }) async {
    _hub.addListener(notifyListeners);
    _hub.onConnected = () => unawaited(_report(force: true));
    _hub.client.on('StartPublishing', (_) => _queue(_startPublishing));
    _hub.client.on('StopPublishing', (_) => _queue(_stopPublishing));
    _hub.client.on(
      'SetTorch',
      (args) => _queue(() => _setTorch(args.firstOrNull == true)),
    );
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
    _queue(_stopPublishing);
    await _publishingChange;
    await _hub.stop();
    await _screen.exitCameraMode();
    await _keepAlive.stop();
  }

  // Start and stop run one at a time, in arrival order, so a quick stop-start pair from the
  // server never overlaps two camera sessions.
  void _queue(Future<void> Function() change) {
    _publishingChange = _publishingChange.then((_) => change());
  }

  // The server may repeat StartPublishing (SPECS.md 5.6); a repeat only re-reports the state.
  Future<void> _startPublishing() async {
    if (!_publishing) {
      _publishing = await _publisher.start();
      notifyListeners();
    }
    await _hub.client.invoke('ReportPublishing', [_publishing]);
  }

  Future<void> _stopPublishing() async {
    if (!_publishing) return;
    await _publisher.stop();
    _publishing = false;
    final torchWasOn = _torchOn;
    _torchOn = false;
    notifyListeners();
    await _hub.client.invoke('ReportPublishing', [false]);
    // Releasing the camera turns the torch off with it.
    if (torchWasOn) await _hub.client.invoke('ReportTorch', [false]);
  }

  // Reports what the torch really is, so viewers never show a state the camera did not reach.
  Future<void> _setTorch(bool on) async {
    if (_publishing && await _publisher.setTorch(on)) {
      _torchOn = on;
      notifyListeners();
    }
    await _hub.client.invoke('ReportTorch', [_torchOn]);
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
