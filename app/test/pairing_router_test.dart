import 'package:flutter_test/flutter_test.dart';
import 'package:recam/camera/camera_pairing_controller.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/pinned_http_overrides.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/core/pairing/pairing_link.dart';
import 'package:recam/core/pairing/pairing_service.dart';
import 'package:recam/core/storage/credential_store.dart';
import 'package:recam/pairing_router.dart';
import 'package:recam/viewer/viewer_pairing_controller.dart';

import 'support/fakes.dart';

void main() {
  late FakeApiClient api;
  late MemoryCredentialStore store;
  late CameraPairingController camera;
  late ViewerPairingController viewer;
  late PairingRouter router;

  setUp(() {
    api = FakeApiClient()..healthyHosts = {'192.168.0.10'};
    store = MemoryCredentialStore();
    final pairing = PairingService(
      api: api,
      store: store,
      pins: PinnedHttpOverrides(),
    );
    camera = CameraPairingController(pairing: pairing);
    viewer = ViewerPairingController(pairing: pairing);
    router = PairingRouter(camera: camera, viewer: viewer);
  });

  tearDown(() {
    camera.dispose();
    viewer.dispose();
  });

  Future<PairingLinkTarget?> scan(String code, PairingLinkTarget from) async {
    await camera.load();
    await viewer.load();
    final target = router.targetOf(code, from: from);
    if (target != null) {
      await router.pair(
        target,
        code,
        cameraName: 'Kitchen',
        viewerName: 'Viewer',
      );
    }
    return target;
  }

  group('PairingRouter', () {
    test('cameraQr_fromWatchTab_pairsTheCameraTab', () async {
      // arrange
      api.pairResult = ApiSuccess(pairResult(role: DeviceRole.camera));

      // act
      final target = await scan(
        pairingQr(role: 'camera'),
        PairingLinkTarget.viewer,
      );

      // assert
      expect(target, PairingLinkTarget.camera);
      expect(api.pairCalls.single.expectedRoles, {DeviceRole.camera});
      expect(api.pairCalls.single.name, 'Kitchen');
      expect(camera.state, isA<CameraPaired>());
      expect(viewer.state, isA<ViewerNotPaired>());
    });

    test('viewerQr_fromCameraTab_pairsTheWatchTab', () async {
      // arrange
      api.pairResult = ApiSuccess(pairResult(role: DeviceRole.viewer));

      // act
      final target = await scan(
        pairingQr(role: 'viewer'),
        PairingLinkTarget.camera,
      );

      // assert
      expect(target, PairingLinkTarget.viewer);
      expect(api.pairCalls.single.expectedRoles, {
        DeviceRole.owner,
        DeviceRole.viewer,
      });
      expect(viewer.state, isA<ViewerPaired>());
      expect(camera.state, isA<CameraNotPaired>());
    });

    test('monitorQr_fromCameraTab_pairsTheWatchTab', () async {
      // arrange
      api.pairResult = ApiSuccess(pairResult());

      // act
      final target = await scan(
        pairingQr(role: 'viewer'),
        PairingLinkTarget.camera,
      );

      // assert
      expect(target, PairingLinkTarget.viewer);
      expect(viewer.state, isA<ViewerPaired>());
    });

    test('cameraQr_fromCameraTab_pairsTheCameraTab', () async {
      // arrange
      api.pairResult = ApiSuccess(pairResult(role: DeviceRole.camera));

      // act
      final target = await scan(
        pairingQr(role: 'camera'),
        PairingLinkTarget.camera,
      );

      // assert
      expect(target, PairingLinkTarget.camera);
      expect(camera.state, isA<CameraPaired>());
    });

    test('notAReCamCode_showsTheErrorOnTheTabThatRead', () async {
      // arrange
      // (no server call expected)

      // act
      final target = await scan('hello', PairingLinkTarget.camera);

      // assert
      expect(target, PairingLinkTarget.camera);
      expect(
        (camera.state as CameraNotPaired).lastFailure,
        PairingFailure.invalidQr,
      );
      expect(api.pairCalls, isEmpty);
    });

    test('codeForAlreadyPairedTab_keepsThePairing', () async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());

      // act
      final target = await scan(
        pairingQr(role: 'viewer'),
        PairingLinkTarget.camera,
      );

      // assert
      expect(target, PairingLinkTarget.viewer);
      expect(api.pairCalls, isEmpty);
    });

    test('notAReCamLink_isIgnored', () async {
      // arrange
      // (a link, not read by any tab)

      // act
      final target = router.targetOf('https://example.com');

      // assert
      expect(target, isNull);
    });
  });
}
