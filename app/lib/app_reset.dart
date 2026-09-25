import 'camera/camera_pairing_controller.dart';
import 'viewer/viewer_pairing_controller.dart';

/// Returns the app to a fresh install: both tabs forget their pairing on this phone. The
/// server keeps the devices until they are revoked.
class AppReset {
  AppReset({required this._camera, required this._viewer});

  final CameraPairingController _camera;
  final ViewerPairingController _viewer;

  Future<void> reset() async {
    await _camera.reset();
    await _viewer.reset();
  }
}
