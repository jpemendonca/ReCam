import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/viewer/add_device_controller.dart';

import '../support/fakes.dart';

PairingTokenResult _token(String qrUri, {int seconds = 3}) =>
    PairingTokenResult(
      id: 'token-$qrUri',
      qrUri: qrUri,
      validFor: Duration(seconds: seconds),
    );

void main() {
  late FakeApiClient api;
  late AddDeviceController controller;

  setUp(() {
    api = FakeApiClient();
    controller = AddDeviceController(
      api: api,
      session: pairedSession(),
      role: DeviceRole.camera,
    );
  });

  group('AddDeviceController', () {
    test('start_withOwnerSession_showsQrUriWithFullValidity', () async {
      // arrange
      api.tokenResults.add(ApiSuccess(_token('recam://pair?a', seconds: 600)));

      // act
      await controller.start();

      // assert
      final state = controller.state as AddDeviceReady;
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
      final state = controller.state as AddDeviceFailed;
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
      final state = controller.state as AddDeviceReady;
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
      final state = controller.state as AddDeviceReady;
      expect(state.qrUri, 'recam://pair?b');
      expect(api.tokenCalls, 2);
      controller.dispose();
    });

    test('start_forViewer_asksForAViewerQr', () async {
      // arrange
      final viewerController = AddDeviceController(
        api: api,
        session: pairedSession(role: DeviceRole.viewer),
        role: DeviceRole.viewer,
      );
      api.tokenResults.add(ApiSuccess(_token('recam://pair?r=viewer')));

      // act
      await viewerController.start();

      // assert
      expect(api.tokenRoles, [DeviceRole.viewer]);
      expect(
        (viewerController.state as AddDeviceReady).qrUri,
        'recam://pair?r=viewer',
      );
      viewerController.dispose();
      controller.dispose();
    });

    testWidgets('whenAnotherPhonePairs_reportsPaired', (tester) async {
      // arrange
      api
        ..tokenResults.add(ApiSuccess(_token('recam://pair?a', seconds: 600)))
        ..tokenUsedResults.addAll([ApiSuccess(false), ApiSuccess(true)]);
      await controller.start();
      await tester.pump(const Duration(seconds: 2));
      final whileWaiting = controller.state;

      // act
      await tester.pump(const Duration(seconds: 2));

      // assert
      expect(whileWaiting, isA<AddDeviceReady>());
      expect(controller.state, isA<AddDevicePaired>());
      expect(api.tokenUsedCalls, [
        'token-recam://pair?a',
        'token-recam://pair?a',
      ]);
      controller.dispose();
    });

    testWidgets('afterPaired_stopsAsking', (tester) async {
      // arrange
      api
        ..tokenResults.add(ApiSuccess(_token('recam://pair?a', seconds: 600)))
        ..tokenUsedResults.add(ApiSuccess(true));
      await controller.start();
      await tester.pump(const Duration(seconds: 2));

      // act
      await tester.pump(const Duration(seconds: 10));

      // assert
      expect(api.tokenUsedCalls, hasLength(1));
      controller.dispose();
    });
  });
}
