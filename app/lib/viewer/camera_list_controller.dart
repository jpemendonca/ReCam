import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/media/webrtc_viewer.dart';
import '../core/network/api_client.dart';
import '../core/network/hub_client.dart';
import '../core/network/hub_session.dart';
import '../core/storage/adjustment_store.dart';
import '../core/storage/credential_store.dart';
import 'brighten_controller.dart';
import 'live_view_controller.dart';
import 'recording_timeline_controller.dart';

typedef CameraListFactory = CameraListController Function(
  PairedSession session,
);

sealed class CameraListState {}

final class CameraListLoading extends CameraListState {}

final class CameraListLoaded extends CameraListState {
  CameraListLoaded(this.cameras);

  final List<CameraInfo> cameras;
}

final class CameraListFailed extends CameraListState {}

/// The cameras a viewer sees. Loads the list from the API and keeps it current with
/// `CameraStatusChanged` from the hub. Every (re)connection reloads the list, because
/// messages sent while offline are lost.
class CameraListController extends ChangeNotifier {
  CameraListController({
    required this._api,
    required this._session,
    required this.hub,
    required this._viewerFactory,
    required this._timelineFactory,
    required this._adjustments,
  });

  final ApiClient _api;
  final PairedSession _session;
  final WebRtcViewerFactory _viewerFactory;
  final RecordingTimelineFactory _timelineFactory;
  final AdjustmentStore _adjustments;

  /// The viewer's hub connection; the live view reuses it for its watch lease.
  final HubSession hub;

  CameraListState _state = CameraListLoading();
  bool _credentialRefused = false;
  bool _refreshing = false;

  CameraListState get state => _state;

  /// True while a reload from the API is on its way.
  bool get refreshing => _refreshing;

  bool get connected => hub.connected;

  /// Ticks when the server says a device paired, left, came online or went offline, so the
  /// Devices tab loads its list again.
  final devicesChanged = ValueNotifier(0);

  /// True when the server refused this viewer's pairing; the list can never load again.
  bool get pairingLost => _credentialRefused || hub.rejected;

  Future<void> start() async {
    hub.client.on('CameraStatusChanged', _onStatusChanged);
    hub.client.on('CameraRemoved', _onCameraRemoved);
    hub.client.on('DevicesChanged', (_) => devicesChanged.value++);
    hub.onConnected = () => unawaited(refresh());
    hub.addListener(notifyListeners);
    hub.start();
    await refresh();
  }

  /// Reloads the list from the API. A failure keeps the list already shown.
  Future<void> refresh() async {
    _refreshing = true;
    notifyListeners();
    final result = await _api.cameras(_session.serverUrl, _session.credential);
    _refreshing = false;
    notifyListeners();
    switch (result) {
      case ApiSuccess(:final value):
        _setState(CameraListLoaded(_sorted(value)));
      case ApiFailure(kind: ApiFailureKind.unauthorized):
        _credentialRefused = true;
        notifyListeners();
      case ApiFailure():
        if (_state is! CameraListLoaded) _setState(CameraListFailed());
    }
  }

  /// Turns "record always" on or off. False when the server refuses; the list shows the new
  /// state once the server confirms it.
  Future<bool> setRecording(String cameraId, bool enabled) async =>
      isHubSuccess(
        await hub.client.invoke('SetRecording', [cameraId, enabled]),
      );

  CameraInfo? camera(String cameraId) => switch (_state) {
    CameraListLoaded(:final cameras) =>
      cameras.where((camera) => camera.id == cameraId).firstOrNull,
    _ => null,
  };

  /// How many cameras the list has; zero until it loads.
  int get cameraCount => switch (_state) {
    CameraListLoaded(:final cameras) => cameras.length,
    _ => 0,
  };

  /// How many cameras have "Record always" on; they share the recording space.
  int get recordingCameras => switch (_state) {
    CameraListLoaded(:final cameras) =>
      cameras.where((camera) => camera.recording).length,
    _ => 0,
  };

  /// The recordings of one camera.
  RecordingTimelineController openRecordings(String cameraId) =>
      _timelineFactory(cameraId);

  /// One camera's "Brighten" setting, for its live view and its recordings.
  BrightenController openBrighten(String cameraId) =>
      BrightenController(store: _adjustments, cameraId: cameraId);

  // The sound as the person left the last live view.
  bool _liveMuted = false;

  /// A live view of one camera, sharing this list's hub connection for the watch lease.
  LiveViewController openLive(String cameraId) => LiveViewController(
    hub: hub,
    viewer: _viewerFactory(cameraId),
    cameraId: cameraId,
    torchOn: camera(cameraId)?.torchOn ?? false,
    muted: _liveMuted,
    onMutedChanged: (muted) => _liveMuted = muted,
  );

  Future<void> stop() async {
    hub.removeListener(notifyListeners);
    await hub.stop();
  }

  void _onStatusChanged(List<Object?> args) {
    final camera = CameraInfo.tryParse(args.firstOrNull);
    final state = _state;
    if (camera == null || state is! CameraListLoaded) return;
    final others = state.cameras.where((existing) => existing.id != camera.id);
    _setState(CameraListLoaded(_sorted([...others, camera])));
  }

  void _onCameraRemoved(List<Object?> args) {
    final id = args.firstOrNull;
    final state = _state;
    if (id is! String || state is! CameraListLoaded) return;
    _setState(
      CameraListLoaded([
        for (final camera in state.cameras)
          if (camera.id.toLowerCase() != id.toLowerCase()) camera,
      ]),
    );
  }

  static List<CameraInfo> _sorted(List<CameraInfo> cameras) =>
      [...cameras]..sort((a, b) => a.name.compareTo(b.name));

  void _setState(CameraListState state) {
    _state = state;
    notifyListeners();
  }

  @override
  void dispose() {
    hub.removeListener(notifyListeners);
    devicesChanged.dispose();
    super.dispose();
  }
}
