import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/media/webrtc_viewer.dart';
import '../core/network/hub_client.dart';
import '../core/network/hub_session.dart';

sealed class LiveViewState {}

final class LiveConnecting extends LiveViewState {}

final class LivePlaying extends LiveViewState {}

final class LiveFailed extends LiveViewState {}

/// One live view: holds a watch lease on the camera while open, and retries WHEP while the
/// camera is still opening (the stream only exists once it publishes). Also switches the
/// camera's torch and shows what the camera reports about it.
class LiveViewController extends ChangeNotifier {
  LiveViewController({
    required this._hub,
    required this._viewer,
    required this.cameraId,
    Delay? delay,
    this.maxAttempts = 20,
    this.retryInterval = const Duration(seconds: 1),
  }) : _delay = delay ?? Future<void>.delayed;

  final HubSession _hub;
  final WebRtcViewer _viewer;
  final String cameraId;
  final Delay _delay;
  final int maxAttempts;
  final Duration retryInterval;

  LiveViewState _state = LiveConnecting();
  bool _torchOn = false;
  bool _closed = false;
  int _generation = 0;

  LiveViewState get state => _state;

  WebRtcViewer get viewer => _viewer;

  bool get torchOn => _torchOn;

  Future<void> start() async {
    _viewer.onEnded = () => unawaited(_retryAfterDrop());
    _hub.client.on('TorchChanged', _onTorchChanged);
    await _hub.client.invoke('WatchCamera', [cameraId]);
    await _connect();
  }

  /// Tries again after a failure, keeping the same lease.
  Future<void> retry() async {
    await _hub.client.invoke('WatchCamera', [cameraId]);
    await _connect();
  }

  /// Asks the camera to switch its torch. Returns false when the server refuses (the camera
  /// is not sending video). The new state arrives later, as the camera reports it.
  Future<bool> setTorch(bool on) async =>
      isHubSuccess(await _hub.client.invoke('SetTorch', [cameraId, on]));

  Future<void> close() async {
    if (_closed) return;
    _closed = true;
    _generation++;
    _hub.client.off('TorchChanged');
    await _viewer.dispose();
    await _hub.client.invoke('UnwatchCamera', [cameraId]);
  }

  Future<void> _connect() async {
    final generation = ++_generation;
    _setState(LiveConnecting());
    for (var attempt = 0; attempt < maxAttempts; attempt++) {
      if (_closed || generation != _generation) return;
      if (await _viewer.start()) {
        if (!_closed && generation == _generation) _setState(LivePlaying());
        return;
      }
      await _delay(retryInterval);
    }
    if (!_closed && generation == _generation) _setState(LiveFailed());
  }

  void _onTorchChanged(List<Object?> args) {
    final id = args.firstOrNull;
    final on = args.elementAtOrNull(1);
    if (id is! String || on is! bool) return;
    if (id.toLowerCase() != cameraId.toLowerCase() || on == _torchOn) return;
    _torchOn = on;
    notifyListeners();
  }

  Future<void> _retryAfterDrop() async {
    if (_closed) return;
    await _viewer.stop();
    await _connect();
  }

  void _setState(LiveViewState state) {
    _state = state;
    notifyListeners();
  }
}
