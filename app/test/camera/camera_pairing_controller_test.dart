import 'package:flutter_test/flutter_test.dart';
import 'package:recam/camera/camera_pairing_controller.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/pinned_http_overrides.dart';
import 'package:recam/core/pairing/device_name_validator.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/core/pairing/pairing_service.dart';
import 'package:recam/core/storage/credential_store.dart';

import '../support/fakes.dart';

void main() {
  late FakeApiClient api;
  late MemoryCredentialStore store;
  late CameraPairingController controller;

  setUp(() {
    api = FakeApiClient();
    store = MemoryCredentialStore();
    controller = CameraPairingController(
      pairing: PairingService(
        api: api,
        store: store,
        pins: PinnedHttpOverrides(),
      ),
    );
  });

  tearDown(() => controller.dispose());

  group('CameraPairingController.submitQr', () {
    test('withCameraToken_pairsAndSavesCredentialInCameraSlot', () async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult(role: DeviceRole.camera));

      // act
      await controller.submitQr(pairingQr(), name: '  Kitchen ');

      // assert
      final state = controller.state as CameraPaired;
      expect(state.session.role, DeviceRole.camera);
      expect(api.pairCalls.single.name, 'Kitchen');
      expect(store.sessions[PairingSlot.camera]?.role, DeviceRole.camera);
      expect(store.sessions[PairingSlot.viewer]?.role, DeviceRole.owner);
    });

    test('withOwnerToken_staysUnpairedWithWrongRole', () async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult());

      // act
      await controller.submitQr(pairingQr(), name: 'Kitchen');

      // assert
      final state = controller.state as CameraNotPaired;
      expect(state.lastFailure, PairingFailure.wrongRole);
      expect(store.sessions, isEmpty);
    });
  });

  group('CameraPairingController.checkName', () {
    test('withValidName_returnsTrue', () {
      // arrange
      const name = 'Garage';

      // act
      final valid = controller.checkName(name);

      // assert
      expect(valid, isTrue);
    });

    test('withBlankName_reportsEmptyError', () {
      // arrange
      const name = '   ';

      // act
      final valid = controller.checkName(name);

      // assert
      expect(valid, isFalse);
      final state = controller.state as CameraNotPaired;
      expect(state.nameErrors, [DeviceNameError.empty]);
    });
  });

  group('CameraPairingController.load', () {
    test('withSavedCameraSession_becomesPaired', () async {
      // arrange
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );

      // act
      await controller.load();

      // assert
      expect(controller.state, isA<CameraPaired>());
    });

    test('withOnlyViewerSession_becomesNotPaired', () async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());

      // act
      await controller.load();

      // assert
      expect(controller.state, isA<CameraNotPaired>());
    });
  });

  group('CameraPairingController pairing lost', () {
    test('load_withRevokedCredential_asksToPairAgain', () async {
      // arrange
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );
      api.meResult = ApiFailure(ApiFailureKind.unauthorized);

      // act
      await controller.load();

      // assert
      final state = controller.state as CameraNotPaired;
      expect(state.lastFailure, PairingFailure.pairingLost);
    });

    test('forget_deletesTheCameraPairingOnly', () async {
      // arrange
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );
      await store.write(PairingSlot.viewer, pairedSession());

      // act
      await controller.forget();

      // assert
      expect(store.sessions.keys, [PairingSlot.viewer]);
      final state = controller.state as CameraNotPaired;
      expect(state.lastFailure, PairingFailure.pairingLost);
    });
  });
}
