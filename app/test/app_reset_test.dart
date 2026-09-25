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
  late FakeApiClient api;
  late MemoryCredentialStore store;
  late CameraPairingController camera;
  late ViewerPairingController viewer;
  late AppReset reset;

  setUp(() {
    api = FakeApiClient();
    store = MemoryCredentialStore();
    final pairing = PairingService(
      api: api,
      store: store,
      pins: PinnedHttpOverrides(),
    );
    camera = CameraPairingController(pairing: pairing);
    viewer = ViewerPairingController(pairing: pairing);
    reset = AppReset(camera: camera, viewer: viewer, api: api);
  });

  tearDown(() {
    camera.dispose();
    viewer.dispose();
  });

  PairedSession session(String credential, DeviceRole role) => PairedSession(
    serverUrl: Uri.parse('https://192.168.0.10:8443'),
    credential: credential,
    deviceId: credential,
    role: role,
    serverName: 'ReCam',
  );

  Future<void> pairBothTabs() async {
    await store.write(
      PairingSlot.camera,
      session('camera.secret', DeviceRole.camera),
    );
    await store.write(
      PairingSlot.viewer,
      session('viewer.secret', DeviceRole.viewer),
    );
    await camera.load();
    await viewer.load();
  }

  group('AppReset.reset', () {
    test('withBothTabsPaired_tellsTheServerAndForgetsBoth', () async {
      // arrange
      await pairBothTabs();

      // act
      await reset.reset();

      // assert
      expect(
        api.leaveCalls,
        unorderedEquals(['camera.secret', 'viewer.secret']),
      );
      expect(store.sessions, isEmpty);
      expect((camera.state as CameraNotPaired).lastFailure, isNull);
      expect((viewer.state as ViewerNotPaired).lastFailure, isNull);
    });

    test('whenServerDoesNotAnswer_forgetsAnyway', () async {
      // arrange
      await pairBothTabs();
      api.leaveResult = false;

      // act
      await reset.reset();

      // assert
      expect(api.leaveCalls, hasLength(2));
      expect(store.sessions, isEmpty);
    });

    test('withNothingPaired_doesNotCallTheServer', () async {
      // arrange
      await camera.load();
      await viewer.load();

      // act
      await reset.reset();

      // assert
      expect(api.leaveCalls, isEmpty);
    });
  });
}
