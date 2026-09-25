import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/app.dart';
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
  late CameraPairingController cameraPairing;
  late ViewerPairingController viewerPairing;

  setUp(() {
    api = FakeApiClient();
    store = MemoryCredentialStore();
    final pairing = PairingService(
      api: api,
      store: store,
      pins: PinnedHttpOverrides(),
    );
    cameraPairing = CameraPairingController(pairing: pairing);
    viewerPairing = ViewerPairingController(pairing: pairing);
  });

  tearDown(() {
    cameraPairing.dispose();
    viewerPairing.dispose();
  });

  Future<void> openApp(WidgetTester tester) async {
    await cameraPairing.load();
    await viewerPairing.load();
    await tester.pumpWidget(
      RecamApp(
        cameraPairing: cameraPairing,
        viewerPairing: viewerPairing,
        api: api,
      ),
    );
    await tester.pumpAndSettle();
  }

  group('RecamApp', () {
    testWidgets('onStart_showsCameraTabWithNameFieldAndScanButton', (
      tester,
    ) async {
      // arrange
      // (empty store)

      // act
      await openApp(tester);

      // assert
      expect(find.text('Scan QR code'), findsOneWidget);
      expect(find.widgetWithText(TextField, 'Camera'), findsOneWidget);
    });

    testWidgets('onCameraTab_withPairedCamera_showsStartCameraMode', (
      tester,
    ) async {
      // arrange
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );

      // act
      await openApp(tester);

      // assert
      expect(find.text('Role: camera'), findsOneWidget);
      expect(find.text('Start camera mode'), findsOneWidget);
    });

    testWidgets('onCameraTab_withBlankName_showsErrorAndStaysOnTab', (
      tester,
    ) async {
      // arrange
      await openApp(tester);
      await tester.enterText(find.byType(TextField), '   ');

      // act
      await tester.tap(find.text('Scan QR code'));
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Enter a name.'), findsOneWidget);
    });

    testWidgets('whenTappingWatchTab_showsScanButtonWhileNotPaired', (
      tester,
    ) async {
      // arrange
      await openApp(tester);

      // act
      await tester.tap(find.byIcon(Icons.live_tv_outlined));
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Scan QR code'), findsOneWidget);
    });

    testWidgets('whenTappingWatchTab_showsServerAndRoleWhenPaired', (
      tester,
    ) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      await openApp(tester);

      // act
      await tester.tap(find.byIcon(Icons.live_tv_outlined));
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Paired with Recam'), findsOneWidget);
      expect(find.text('Role: owner'), findsOneWidget);
      expect(find.text('Add camera'), findsOneWidget);
    });
  });
}
