import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/pinned_http_overrides.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/core/pairing/pairing_service.dart';
import 'package:recam/core/storage/credential_store.dart';
import 'package:recam/viewer/viewer_pairing_controller.dart';

import '../support/fakes.dart';

void main() {
  late FakeApiClient api;
  late MemoryCredentialStore store;
  late ViewerPairingController controller;

  setUp(() {
    api = FakeApiClient();
    store = MemoryCredentialStore();
    controller = ViewerPairingController(
      pairing: PairingService(
        api: api,
        store: store,
        pins: PinnedHttpOverrides(),
      ),
    );
  });

  tearDown(() => controller.dispose());

  group('ViewerPairingController.submitQr', () {
    test('withOwnerToken_pairsAndSavesCredentialInViewerSlot', () async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult());

      // act
      await controller.submitQr(pairingQr(), deviceName: 'Owner phone');

      // assert
      final state = controller.state as ViewerPaired;
      expect(state.session.role, DeviceRole.owner);
      expect(state.session.fingerprint, fingerprint);
      expect(api.pairCalls.single.token, 'tok');
      expect(api.pairCalls.single.name, 'Owner phone');
      expect(
        store.sessions[PairingSlot.viewer]?.credential,
        '0123456789abcdef0123456789abcdef.secret',
      );
      expect(store.sessions.containsKey(PairingSlot.camera), isFalse);
    });

    test('withCameraToken_staysUnpairedWithWrongRole', () async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult(role: DeviceRole.camera));

      // act
      await controller.submitQr(pairingQr(), deviceName: 'Owner phone');

      // assert
      final state = controller.state as ViewerNotPaired;
      expect(state.lastFailure, PairingFailure.wrongRole);
      expect(store.sessions, isEmpty);
    });

    test('withUnreachableServer_staysUnpairedWithReason', () async {
      // arrange
      final qr = pairingQr();

      // act
      await controller.submitQr(qr, deviceName: 'Owner phone');

      // assert
      final state = controller.state as ViewerNotPaired;
      expect(state.lastFailure, PairingFailure.serverUnreachable);
    });
  });

  group('ViewerPairingController.load', () {
    test('withoutSavedSession_becomesNotPaired', () async {
      // arrange
      // (empty store)

      // act
      await controller.load();

      // assert
      expect((controller.state as ViewerNotPaired).lastFailure, isNull);
    });

    test('withSavedSession_becomesPaired', () async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      api.meResult = ApiFailure(ApiFailureKind.unreachable);

      // act
      await controller.load();

      // assert
      expect(controller.state, isA<ViewerPaired>());
    });

    test('withRevokedSession_becomesNotPaired', () async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      api.meResult = ApiFailure(ApiFailureKind.unauthorized);

      // act
      await controller.load();

      // assert
      expect(controller.state, isA<ViewerNotPaired>());
      expect(store.sessions, isEmpty);
    });
  });
}
