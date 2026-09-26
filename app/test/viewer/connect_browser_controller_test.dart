import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/viewer/connect_browser_controller.dart';

import '../support/fakes.dart';

const _code =
    'recam://connect-browser?v=1&l=0123456789abcdef0123456789abcdef&s=secret';

void main() {
  late FakeApiClient api;
  late ConnectBrowserController controller;

  setUp(() {
    api = FakeApiClient();
    controller = ConnectBrowserController(api: api, session: pairedSession());
  });

  tearDown(() => controller.dispose());

  group('ConnectBrowserController', () {
    test('approve_withBrowserCode_approvesOnServer', () async {
      // act
      await controller.approve(_code);

      // assert
      expect(controller.state, isA<ConnectBrowserDone>());
      expect(
        api.approvedLinks.single.linkId,
        '0123456789abcdef0123456789abcdef',
      );
      expect(api.approvedLinks.single.secret, 'secret');
    });

    test('approve_withPairingCode_saysWrongCode', () async {
      // act
      await controller.approve('recam://pair?v=1&t=token&r=camera');

      // assert
      expect(controller.state, isA<ConnectBrowserWrongCode>());
      expect(api.approvedLinks, isEmpty);
    });

    test('approve_expiredCode_reportsRejected', () async {
      // arrange
      api.approveLinkFailure = ApiFailureKind.rejected;

      // act
      await controller.approve(_code);

      // assert
      final state = controller.state as ConnectBrowserFailed;
      expect(state.kind, ApiFailureKind.rejected);
    });
  });
}
