import 'package:flutter_test/flutter_test.dart';
import 'package:recam/app_reset.dart';
import 'package:recam/camera/camera_pairing_controller.dart';
import 'package:recam/core/network/pinned_http_overrides.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/core/pairing/pairing_service.dart';
import 'package:recam/core/storage/credential_store.dart';
import 'package:recam/viewer/viewer_pairing_controller.dart';

import 'support/fakes.dart';

void main() {
  group('AppReset.reset', () {
    test('withBothTabsPaired_forgetsBothAndLeavesThemNotPaired', () async {
      // arrange
      final store = MemoryCredentialStore();
      final pairing = PairingService(
        api: FakeApiClient(),
        store: store,
        pins: PinnedHttpOverrides(),
      );
      final camera = CameraPairingController(pairing: pairing);
      final viewer = ViewerPairingController(pairing: pairing);
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );
      await store.write(PairingSlot.viewer, pairedSession());
      await camera.load();
      await viewer.load();
      final reset = AppReset(camera: camera, viewer: viewer);

      // act
      await reset.reset();

      // assert
      expect(store.sessions, isEmpty);
      expect((camera.state as CameraNotPaired).lastFailure, isNull);
      expect((viewer.state as ViewerNotPaired).lastFailure, isNull);
    });
  });
}
