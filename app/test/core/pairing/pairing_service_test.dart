import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/pinned_http_overrides.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/core/pairing/pairing_service.dart';
import 'package:recam/core/storage/credential_store.dart';

import '../../support/fakes.dart';

void main() {
  late FakeApiClient api;
  late MemoryCredentialStore store;
  late PairingService service;

  setUp(() {
    api = FakeApiClient();
    store = MemoryCredentialStore();
    service = PairingService(
      api: api,
      store: store,
      pins: PinnedHttpOverrides(),
    );
  });

  Future<PairingOutcome> pair(String rawQr) => service.pairFromQr(
    rawQr: rawQr,
    deviceName: 'My phone',
    slot: PairingSlot.viewer,
    acceptedRoles: {DeviceRole.owner, DeviceRole.viewer},
  );

  group('PairingService.pairFromQr', () {
    test('withTokenForOtherTab_returnsWrongRoleAndSavesNothing', () async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiFailure(ApiFailureKind.conflict);

      // act
      final outcome = await pair(pairingQr());

      // assert
      expect((outcome as PairingFailed).reason, PairingFailure.wrongRole);
      expect(api.pairCalls.single.expectedRoles, {
        DeviceRole.owner,
        DeviceRole.viewer,
      });
      expect(await store.read(PairingSlot.viewer), isNull);
    });

    test('withSecondUrlReachable_pairsThroughIt', () async {
      // arrange
      api
        ..healthyHosts = {'10.0.0.5'}
        ..pairResult = ApiSuccess(pairResult());
      final qr = pairingQr(
        urls: ['https://192.168.0.10:8443', 'https://10.0.0.5:8443'],
      );

      // act
      final outcome = await pair(qr);

      // assert
      final session = (outcome as PairingSucceeded).session;
      expect(session.serverUrl, Uri.parse('https://10.0.0.5:8443'));
      expect(api.pairCalls.single.baseUrl.host, '10.0.0.5');
    });

    test('withNoUrlReachable_returnsServerUnreachable', () async {
      // arrange
      final qr = pairingQr();

      // act
      final outcome = await pair(qr);

      // assert
      expect(
        (outcome as PairingFailed).reason,
        PairingFailure.serverUnreachable,
      );
      expect(api.pairCalls, isEmpty);
    });

    test('withRejectedToken_returnsTokenRejectedAndSavesNothing', () async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiFailure(ApiFailureKind.unauthorized);

      // act
      final outcome = await pair(pairingQr());

      // assert
      expect((outcome as PairingFailed).reason, PairingFailure.tokenRejected);
      expect(store.sessions, isEmpty);
    });

    test('withRoleNotAccepted_returnsWrongRoleAndSavesNothing', () async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult(role: DeviceRole.camera));

      // act
      final outcome = await pair(pairingQr());

      // assert
      expect((outcome as PairingFailed).reason, PairingFailure.wrongRole);
      expect(store.sessions, isEmpty);
    });

    test('withInvalidQr_returnsInvalidQrWithoutCallingServer', () async {
      // arrange
      const qr = 'https://example.com';

      // act
      final outcome = await pair(qr);

      // assert
      expect((outcome as PairingFailed).reason, PairingFailure.invalidQr);
      expect(api.healthCalls, isEmpty);
    });
  });

  group('PairingService.verify', () {
    test('withRevokedCredential_forgetsTheSession', () async {
      // arrange
      final session = pairedSession();
      await store.write(PairingSlot.viewer, session);
      api.meResult = ApiFailure(ApiFailureKind.unauthorized);

      // act
      final status = await service.verify(PairingSlot.viewer, session);

      // assert
      expect(status, SessionStatus.revoked);
      expect(store.sessions, isEmpty);
    });

    test('withServerOffline_keepsTheSession', () async {
      // arrange
      final session = pairedSession();
      await store.write(PairingSlot.viewer, session);
      api.meResult = ApiFailure(ApiFailureKind.unreachable);

      // act
      final status = await service.verify(PairingSlot.viewer, session);

      // assert
      expect(status, SessionStatus.unknown);
      expect(store.sessions, isNotEmpty);
    });
  });
}
