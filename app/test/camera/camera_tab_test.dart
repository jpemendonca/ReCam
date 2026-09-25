import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/camera/camera_mode_controller.dart';
import 'package:recam/camera/camera_mode_screen.dart';
import 'package:recam/camera/camera_pairing_controller.dart';
import 'package:recam/camera/camera_tab.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/hub_session.dart';
import 'package:recam/core/network/pinned_http_overrides.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/core/pairing/pairing_service.dart';
import 'package:recam/core/storage/credential_store.dart';
import 'package:recam/l10n/generated/app_localizations.dart';

import '../support/fakes.dart';

void main() {
  late FakeApiClient api;
  late MemoryCredentialStore store;
  late CameraPairingController pairing;

  setUp(() {
    api = FakeApiClient();
    store = MemoryCredentialStore();
    pairing = CameraPairingController(
      pairing: PairingService(
        api: api,
        store: store,
        pins: PinnedHttpOverrides(),
      ),
    );
  });

  tearDown(() => pairing.dispose());

  Future<void> openTab(WidgetTester tester) async {
    await pairing.load();
    await tester.pumpWidget(
      MaterialApp(
        localizationsDelegates: const [
          AppLocalizations.delegate,
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
        ],
        supportedLocales: AppLocalizations.supportedLocales,
        home: Scaffold(
          body: CameraTab(
            pairing: pairing,
            cameraMode: (_) => CameraModeController(
              hub: HubSession(client: FakeHubClient(), delay: (_) async {}),
              battery: FakeBatteryReader(),
              screen: FakeScreenController(),
              keepAlive: FakeKeepAlive(),
              publisher: FakePublisher(),
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('CameraTab', () {
    testWidgets('afterPairingFromQr_opensCameraModeRightAway', (tester) async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult(role: DeviceRole.camera));
      await openTab(tester);

      // act
      await pairing.submitQr(pairingQr(role: 'camera'), name: 'Kitchen');
      await tester.pumpAndSettle();

      // assert
      expect(find.byType(CameraModeScreen), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('afterFailedPairing_staysOnPairingScreen', (tester) async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiFailure(ApiFailureKind.unauthorized);
      await openTab(tester);

      // act
      await pairing.submitQr(pairingQr(role: 'camera'), name: 'Kitchen');
      await tester.pumpAndSettle();

      // assert
      expect(find.byType(CameraModeScreen), findsNothing);
      expect(find.text('Scan QR code'), findsOneWidget);
    });

    testWidgets('withSavedPairing_waitsForStartCameraMode', (tester) async {
      // arrange
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );

      // act
      await openTab(tester);

      // assert
      expect(find.byType(CameraModeScreen), findsNothing);
      expect(find.text('Start camera mode'), findsOneWidget);
    });
  });
}
