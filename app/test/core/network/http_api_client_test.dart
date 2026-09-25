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

  group('HttpApiClient.createCameraPairingToken', () {
    test('withCreatedResponse_countsValidityFromServerDate', () async {
      // arrange
      late http.Request sent;
      final client = HttpApiClient(
        MockClient((request) async {
          sent = request;
          return http.Response(
            jsonEncode({
              'qrUri': 'recam://pair?v=1',
              'expiresAt': '2026-09-24T12:10:00+00:00',
            }),
            201,
            headers: {'date': 'Thu, 24 Sep 2026 12:00:00 GMT'},
          );
        }),
      );

      // act
      final result = await client.createCameraPairingToken(
        baseUrl,
        'id.secret',
      );

      // assert
      final token = (result as ApiSuccess<PairingTokenResult>).value;
      expect(token.qrUri, 'recam://pair?v=1');
      expect(token.validFor, const Duration(minutes: 10));
      expect(sent.url.path, '/api/pairing-tokens');
      expect(sent.headers['authorization'], 'Bearer id.secret');
      expect(jsonDecode(sent.body), {'role': 'camera'});
    });

    test('withForbiddenResponse_returnsUnexpectedFailure', () async {
      // arrange
      final client = HttpApiClient(
        MockClient((_) async => http.Response('', 403)),
      );

      // act
      final result = await client.createCameraPairingToken(
        baseUrl,
        'id.secret',
      );

      // assert
      expect(
        (result as ApiFailure<PairingTokenResult>).kind,
        ApiFailureKind.unexpected,
      );
    });
  });
}
