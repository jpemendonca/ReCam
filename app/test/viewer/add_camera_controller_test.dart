import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/viewer/add_camera_controller.dart';

import '../support/fakes.dart';

PairingTokenResult _token(String qrUri, {int seconds = 3}) =>
    PairingTokenResult(
      qrUri: qrUri,
      validFor: Duration(seconds: seconds),
    );

void main() {
  late FakeApiClient api;
  late AddCameraController controller;

  setUp(() {
    api = FakeApiClient();
    controller = AddCameraController(api: api, session: pairedSession());
  });

  group('AddCameraController', () {
    test('start_withOwnerSession_showsQrUriWithFullValidity', () async {
      // arrange
      api.tokenResults.add(ApiSuccess(_token('recam://pair?a', seconds: 600)));

      // act
      await controller.start();

      // assert
      final state = controller.state as AddCameraReady;
      expect(state.qrUri, 'recam://pair?a');
      expect(state.remaining, const Duration(minutes: 10));
      controller.dispose();
    });

    test('start_whenServerFails_showsFailure', () async {
      // arrange
      api.tokenResults.add(ApiFailure(ApiFailureKind.unreachable));

      // act
      await controller.start();

      // assert
      final state = controller.state as AddCameraFailed;
      expect(state.kind, ApiFailureKind.unreachable);
      controller.dispose();
    });

    testWidgets('afterOneSecond_countsDown', (tester) async {
      // arrange
      api.tokenResults.add(ApiSuccess(_token('recam://pair?a')));
      await controller.start();

      // act
      await tester.pump(const Duration(seconds: 1));

      // assert
      final state = controller.state as AddCameraReady;
      expect(state.remaining, const Duration(seconds: 2));
      controller.dispose();
    });

    testWidgets('whenTokenExpires_createsNewQr', (tester) async {
      // arrange
      api.tokenResults
        ..add(ApiSuccess(_token('recam://pair?a')))
        ..add(ApiSuccess(_token('recam://pair?b')));
      await controller.start();

      // act
      await tester.pump(const Duration(seconds: 3));

      // assert
      final state = controller.state as AddCameraReady;
      expect(state.qrUri, 'recam://pair?b');
      expect(api.tokenCalls, 2);
      controller.dispose();
    });
  });
}
