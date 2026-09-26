import 'camera/camera_pairing_controller.dart';
import 'core/network/api_client.dart';
import 'core/storage/credential_store.dart';
import 'viewer/viewer_pairing_controller.dart';

/// Returns the app to a fresh install: each paired tab first takes its device off the server,
/// then forgets its pairing on this phone. A server that does not answer does not stop the
/// reset.
class AppReset {
  AppReset({required this._camera, required this._viewer, required this._api});

  final CameraPairingController _camera;
  final ViewerPairingController _viewer;
  final ApiClient _api;

  Future<void> reset() async {
    final sessions = [
      if (_camera.state case CameraPaired(:final session)) session,
      if (_viewer.state case ViewerPaired(:final session)) session,
    ];
    await Future.wait(sessions.map(_leave));
    await _camera.reset();
    await _viewer.reset();
  }

  /// A phone paired in both roles, from before one role per phone, keeps the Monitor: the
  /// camera leaves the server and is forgotten here.
  Future<void> keepOneRole() async {
    if (_viewer.state is! ViewerPaired) return;
    if (_camera.state case CameraPaired(:final session)) {
      await _leave(session);
      await _camera.reset();
    }
  }

  Future<void> _leave(PairedSession session) =>
      _api.leave(session.serverUrl, session.credential);
}
