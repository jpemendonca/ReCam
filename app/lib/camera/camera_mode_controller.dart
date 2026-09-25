import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/device/battery_reader.dart';
import '../core/device/keep_alive.dart';
import '../core/device/screen_controller.dart';
import '../core/media/camera_capture.dart';
import '../core/media/video_quality.dart';
import '../core/media/webrtc_publisher.dart';
import '../core/network/hub_session.dart';
import '../core/storage/credential_store.dart';

typedef CameraModeFactory = CameraModeController Function(
  PairedSession session,
);

/// Camera mode: stays connected to the hub, reports the battery, and publishes video only
/// while the server asks (someone is watching). Sends a battery report when the connection
/// opens, when the reading changes and at least every [reportInterval].
///
/// The camera is open while something uses it: publishing, the on-screen thumbnail, or both
/// sharing the same track. It is released when neither needs it.
class CameraModeController extends ChangeNotifier {
  CameraModeController({
    required this._hub,
    required this._battery,
    required this._screen,
    required this._keepAlive,
    required this._publisher,
    required this._capture,
    DateTime Function()? now,
    this.reportInterval = const Duration(seconds: 60),
    this.checkInterval = const Duration(seconds: 15),
  }) : _now = now ?? DateTime.now;

  final HubSession _hub;
  final BatteryReader _battery;
  final ScreenController _screen;
  final KeepAlive _keepAlive;
  final WebRtcPublisher _publisher;
  final CameraCapture _capture;
  final DateTime Function() _now;
  final Duration reportInterval;
  final Duration checkInterval;

  Timer? _checkTimer;
  BatteryReading? _lastSent;
  DateTime? _lastSentAt;
  bool _publishing = false;
  bool _previewOpen = false;
  VideoQuality _quality = VideoQuality.full;
  CameraFeed? _feed;
  bool _torchOn = false;
  int _watchers = 0;
  Future<void> _publishingChange = Future.value();

  bool get connected => _hub.connected;

  /// True when the server refused this camera's pairing; camera mode cannot go on.
  bool get pairingLost => _hub.rejected;

  bool get publishing => _publishing;

  /// Reduced while the phone is hot (SPECS.md 11).
  VideoQuality get quality => _quality;

  /// The camera to show as a thumbnail, or null while the thumbnail is closed.
  CameraFeed? get preview => _previewOpen ? _feed : null;

  bool get torchOn => _torchOn;

  /// How many viewers are watching, as the server last said. Zero while disconnected.
  int get watchers => _hub.connected ? _watchers : 0;

  BatteryReading? get lastReading => _lastSent;

  Future<void> start({
    required String notificationTitle,
    required String notificationText,
  }) async {
    _hub.addListener(notifyListeners);
    _hub.onConnected = () => unawaited(_report(force: true));
    _hub.client.on('StartPublishing', (_) => _queue(_startPublishing));
    _hub.client.on('StopPublishing', (_) => _queue(_stopPublishing));
    _hub.client.on('WatchersChanged', _onWatchersChanged);
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

  /// Opens the thumbnail. Reuses the camera when it is already publishing.
  Future<void> openPreview() {
    _queue(_openPreview);
    return _publishingChange;
  }

  /// Closes the thumbnail, and the camera too when nobody watches.
  Future<void> closePreview() {
    _queue(_closePreview);
    return _publishingChange;
  }

  Future<void> stop() async {
    _checkTimer?.cancel();
    _checkTimer = null;
    _hub.removeListener(notifyListeners);
    _queue(_closePreview);
    _queue(_stopPublishing);
    await _publishingChange;
    await _hub.stop();
    await _screen.exitCameraMode();
    await _keepAlive.stop();
  }

  // Start, stop and the thumbnail run one at a time, in arrival order, so a quick stop-start
  // pair from the server never overlaps two camera sessions.
  void _queue(Future<void> Function() change) {
    _publishingChange = _publishingChange.then((_) => change());
  }

  // The server may repeat StartPublishing (SPECS.md 5.6); a repeat only re-reports the state.
  Future<void> _startPublishing() async {
    if (!_publishing) {
      final feed = await _openCamera();
      _publishing = feed != null && await _publisher.start(feed);
      if (!_publishing) await _releaseCameraIfUnused();
      notifyListeners();
    }
    await _hub.client.invoke('ReportPublishing', [_publishing]);
  }

  Future<void> _stopPublishing() async {
    if (!_publishing) return;
    final torchWasOn = _torchOn;
    // The thumbnail may keep the camera open, so the torch is switched off explicitly.
    if (torchWasOn) await _publisher.setTorch(false);
    await _publisher.stop();
    _publishing = false;
    _torchOn = false;
    await _releaseCameraIfUnused();
    notifyListeners();
    await _hub.client.invoke('ReportPublishing', [false]);
    if (torchWasOn) await _hub.client.invoke('ReportTorch', [false]);
  }

  Future<void> _openPreview() async {
    if (_previewOpen) return;
    _previewOpen = await _openCamera() != null;
    notifyListeners();
  }

  Future<void> _closePreview() async {
    if (!_previewOpen) return;
    _previewOpen = false;
    notifyListeners();
    await _releaseCameraIfUnused();
  }

  Future<CameraFeed?> _openCamera() async => _feed ??= await _capture.open();

  Future<void> _releaseCameraIfUnused() async {
    if (_publishing || _previewOpen || _feed == null) return;
    _feed = null;
    await _capture.close();
  }

  // Reports what the torch really is, so viewers never show a state the camera did not reach.
  Future<void> _setTorch(bool on) async {
    if (_publishing && await _publisher.setTorch(on)) {
      _torchOn = on;
      notifyListeners();
    }
    await _hub.client.invoke('ReportTorch', [_torchOn]);
  }

  void _followTemperature(double? temperatureC) {
    final next = VideoQuality.forTemperature(_quality, temperatureC);
    if (next == _quality) return;
    _quality = next;
    notifyListeners();
    _queue(() => _publisher.setQuality(next));
  }

  void _onWatchersChanged(List<Object?> args) {
    final count = args.firstOrNull;
    if (count is! num) return;
    _watchers = count.toInt();
    notifyListeners();
  }

  Future<void> _report({required bool force}) async {
    if (!_hub.connected) return;
    final reading = await _battery.read();
    _followTemperature(reading.temperatureC);
    final lastSentAt = _lastSentAt;
    final due =
        lastSentAt == null || _now().difference(lastSentAt) >= reportInterval;
    if (!force && reading == _lastSent && !due) return;
    await _hub.client.invoke('ReportTelemetry', [
      {
        'batteryLevel': reading.level,
        'isCharging': reading.isCharging,
        'temperatureC': reading.temperatureC,
      },
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
