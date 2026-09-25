import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/http_api_client.dart';
import 'package:recam/core/pairing/device_role.dart';

void main() {
  final baseUrl = Uri.parse('https://192.168.0.10:8443');

  group('HttpApiClient.pair', () {
    test('withExpectedRoles_sendsThemInTheBody', () async {
      // arrange
      late http.Request sent;
      final client = HttpApiClient(
        MockClient((request) async {
          sent = request;
          return http.Response('', 409);
        }),
      );

      // act
      final result = await client.pair(
        baseUrl,
        token: 'tok',
        name: 'Kitchen',
        expectedRoles: {DeviceRole.camera},
      );

      // assert
      expect(jsonDecode(sent.body), {
        'token': 'tok',
        'name': 'Kitchen',
        'expectedRoles': ['camera'],
      });
      expect((result as ApiFailure<PairResult>).kind, ApiFailureKind.conflict);
    });
  });

  group('HttpApiClient.createPairingToken', () {
    test('withCreatedResponse_countsValidityFromServerDate', () async {
      // arrange
      late http.Request sent;
      final client = HttpApiClient(
        MockClient((request) async {
          sent = request;
          return http.Response(
            jsonEncode({
              'id': '0199a1b2-0000-7000-8000-000000000001',
              'qrUri': 'recam://pair?v=1',
              'expiresAt': '2026-09-24T12:10:00+00:00',
            }),
            201,
            headers: {'date': 'Thu, 24 Sep 2026 12:00:00 GMT'},
          );
        }),
      );

      // act
      final result = await client.createPairingToken(
        baseUrl,
        'id.secret',
        DeviceRole.camera,
      );

      // assert
      final token = (result as ApiSuccess<PairingTokenResult>).value;
      expect(token.id, '0199a1b2-0000-7000-8000-000000000001');
      expect(token.qrUri, 'recam://pair?v=1');
      expect(token.validFor, const Duration(minutes: 10));
      expect(sent.url.path, '/api/pairing-tokens');
      expect(sent.headers['authorization'], 'Bearer id.secret');
      expect(jsonDecode(sent.body), {'role': 'camera'});
    });

    test('forViewer_sendsViewerRole', () async {
      // arrange
      late http.Request sent;
      final client = HttpApiClient(
        MockClient((request) async {
          sent = request;
          return http.Response(
            jsonEncode({
              'id': '0199a1b2-0000-7000-8000-000000000001',
              'qrUri': 'recam://pair?v=1',
              'expiresAt': '2026-09-24T12:10:00+00:00',
            }),
            201,
          );
        }),
      );

      // act
      await client.createPairingToken(baseUrl, 'id.secret', DeviceRole.viewer);

      // assert
      expect(jsonDecode(sent.body), {'role': 'viewer'});
    });

    test('withForbiddenResponse_returnsUnexpectedFailure', () async {
      // arrange
      final client = HttpApiClient(
        MockClient((_) async => http.Response('', 403)),
      );

      // act
      final result = await client.createPairingToken(
        baseUrl,
        'id.secret',
        DeviceRole.camera,
      );

      // assert
      expect(
        (result as ApiFailure<PairingTokenResult>).kind,
        ApiFailureKind.unexpected,
      );
    });
  });

  group('HttpApiClient.pairingTokenUsed', () {
    test('withUsedToken_returnsTrue', () async {
      // arrange
      late http.Request sent;
      final client = HttpApiClient(
        MockClient((request) async {
          sent = request;
          return http.Response(jsonEncode({'used': true}), 200);
        }),
      );

      // act
      final result = await client.pairingTokenUsed(baseUrl, 'id.secret', 'abc');

      // assert
      expect((result as ApiSuccess<bool>).value, isTrue);
      expect(sent.url.path, '/api/pairing-tokens/abc');
      expect(sent.headers['authorization'], 'Bearer id.secret');
    });
  });

  group('HttpApiClient.leave', () {
    test('withNoContent_returnsTrue', () async {
      // arrange
      late http.Request sent;
      final client = HttpApiClient(
        MockClient((request) async {
          sent = request;
          return http.Response('', 204);
        }),
      );

      // act
      final left = await client.leave(baseUrl, 'id.secret');

      // assert
      expect(left, isTrue);
      expect(sent.method, 'DELETE');
      expect(sent.url.path, '/api/me');
    });

    test('whenServerIsDown_returnsFalse', () async {
      // arrange
      final client = HttpApiClient(
        MockClient((_) async => throw http.ClientException('down')),
      );

      // act
      final left = await client.leave(baseUrl, 'id.secret');

      // assert
      expect(left, isFalse);
    });
  });

  group('HttpApiClient.recordingQuota', () {
    test('withQuota_readsIt', () async {
      // arrange
      final client = HttpApiClient(
        MockClient(
          (_) async => http.Response(
            jsonEncode({'quotaMb': 2048, 'usedBytes': 10, 'freeBytes': 20}),
            200,
          ),
        ),
      );

      // act
      final result = await client.recordingQuota(baseUrl, 'id.secret');

      // assert
      final quota = (result as ApiSuccess<RecordingQuota>).value;
      expect(quota.megabytes, 2048);
      expect(quota.usedBytes, 10);
      expect(quota.freeBytes, 20);
    });

    test('setRecordingQuota_withNoContent_returnsNoFailure', () async {
      // arrange
      late http.Request sent;
      final client = HttpApiClient(
        MockClient((request) async {
          sent = request;
          return http.Response('', 204);
        }),
      );

      // act
      final failure = await client.setRecordingQuota(baseUrl, 'id.secret', 300);

      // assert
      expect(failure, isNull);
      expect(sent.method, 'PUT');
      expect(jsonDecode(sent.body), {'quotaMb': 300});
    });
  });
}
