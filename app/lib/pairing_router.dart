import 'camera/camera_pairing_controller.dart';
import 'core/pairing/pairing_link.dart';
import 'viewer/viewer_pairing_controller.dart';

/// Sends a pairing code, scanned, pasted or opened as a link, to the tab its role is for, so
/// one reader serves both tabs. A tab that is already paired keeps its pairing.
class PairingRouter {
  PairingRouter({required this._camera, required this._viewer});

  final CameraPairingController _camera;
  final ViewerPairingController _viewer;

  /// The tab a code is for. A code that is not a ReCam pairing code goes to [from], so the tab
  /// that read it shows the error; without [from] (a link) it is ignored.
  PairingLinkTarget? targetOf(String code, {PairingLinkTarget? from}) =>
      pairingLinkTarget(code) ?? from;

  /// Pairs [target] with the code, unless that tab is already paired.
  Future<void> pair(
    PairingLinkTarget target,
    String code, {
    required String cameraName,
    required String viewerName,
  }) async {
    switch (target) {
      case PairingLinkTarget.camera:
        if (_camera.state is CameraNotPaired) {
          await _camera.submitQr(code, name: cameraName);
        }
      case PairingLinkTarget.viewer:
        if (_viewer.state is ViewerNotPaired) {
          await _viewer.submitQr(code, deviceName: viewerName);
        }
    }
  }
}
