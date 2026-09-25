import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/media/webrtc_viewer.dart';
import '../core/network/api_client.dart';
import '../core/network/hub_session.dart';
import '../core/storage/credential_store.dart';
import 'live_view_controller.dart';

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
  });

  final ApiClient _api;
  final PairedSession _session;
  final WebRtcViewerFactory _viewerFactory;

  /// The viewer's hub connection; the live view reuses it for its watch lease.
  final HubSession hub;

  CameraListState _state = CameraListLoading();

  CameraListState get state => _state;

  bool get connected => hub.connected;

  Future<void> start() async {
    hub.client.on('CameraStatusChanged', _onStatusChanged);
    hub.onConnected = () => unawaited(refresh());
    hub.addListener(notifyListeners);
    hub.start();
    await refresh();
  }

  Future<void> refresh() async {
    final result = await _api.cameras(_session.serverUrl, _session.credential);
    switch (result) {
      case ApiSuccess(:final value):
        _setState(CameraListLoaded(_sorted(value)));
      case ApiFailure():
        if (_state is! CameraListLoaded) _setState(CameraListFailed());
    }
  }

  /// A live view of one camera, sharing this list's hub connection for the watch lease.
  LiveViewController openLive(String cameraId) => LiveViewController(
    hub: hub,
    viewer: _viewerFactory(cameraId),
    cameraId: cameraId,
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

  static List<CameraInfo> _sorted(List<CameraInfo> cameras) =>
      [...cameras]..sort((a, b) => a.name.compareTo(b.name));

  void _setState(CameraListState state) {
    _state = state;
    notifyListeners();
  }

  @override
  void dispose() {
    hub.removeListener(notifyListeners);
    super.dispose();
  }
}
