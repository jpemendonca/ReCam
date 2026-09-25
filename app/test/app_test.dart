import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/app.dart';
import 'package:recam/camera/camera_mode_controller.dart';
import 'package:recam/camera/camera_mode_screen.dart';
import 'package:recam/camera/camera_pairing_controller.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/pinned_http_overrides.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/core/pairing/pairing_service.dart';
import 'package:recam/core/storage/credential_store.dart';
import 'package:recam/core/network/hub_session.dart';
import 'package:recam/viewer/camera_list_controller.dart';
import 'package:recam/viewer/viewer_pairing_controller.dart';

import 'support/fakes.dart';

void main() {
  late FakeApiClient api;
  late MemoryCredentialStore store;
  late CameraPairingController cameraPairing;
  late ViewerPairingController viewerPairing;
  late FakeLinkSource links;
  late List<String?> codesToRead;

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
    links = FakeLinkSource();
    codesToRead = [];
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
        cameraMode: (_) => CameraModeController(
          hub: HubSession(client: FakeHubClient(), delay: (_) async {}),
          battery: FakeBatteryReader(),
          screen: FakeScreenController(),
          keepAlive: FakeKeepAlive(),
          publisher: FakePublisher(),
        ),
        cameraList: (session) => CameraListController(
          api: api,
          session: session,
          hub: HubSession(client: FakeHubClient(), delay: (_) async {}),
          viewerFactory: (_) => FakeViewer(),
        ),
        links: links,
        readCode: (_) async => codesToRead.removeAt(0),
        ready: Future.value(),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('RecamApp pairing links', () {
    testWidgets('withOwnerLink_opensWatchTabAndPairsAsOwner', (tester) async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult());
      await openApp(tester);

      // act
      links.controller.add(Uri.parse(pairingQr(role: 'owner')));
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Paired with ReCam'), findsOneWidget);
      expect(api.pairCalls.single.expectedRoles, {
        DeviceRole.owner,
        DeviceRole.viewer,
      });
    });

    testWidgets('withCameraLink_pairsWithDefaultNameAndOpensCameraMode', (
      tester,
    ) async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult(role: DeviceRole.camera));
      await openApp(tester);

      // act
      links.controller.add(Uri.parse(pairingQr(role: 'camera')));
      await tester.pumpAndSettle();

      // assert
      expect(api.pairCalls.single.name, 'Camera');
      expect(api.pairCalls.single.expectedRoles, {DeviceRole.camera});
      expect(find.byType(CameraModeScreen), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('withLinkForAlreadyPairedTab_doesNotPairAgain', (tester) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      await openApp(tester);

      // act
      links.controller.add(Uri.parse(pairingQr(role: 'owner')));
      await tester.pumpAndSettle();

      // assert
      expect(api.pairCalls, isEmpty);
      expect(find.text('Paired with ReCam'), findsOneWidget);
    });
  });

  group('RecamApp', () {
    testWidgets('onStart_withOnlyViewerPaired_showsCameraTabWithScanButton', (
      tester,
    ) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());

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
      expect(find.text('Paired with ReCam'), findsOneWidget);
      expect(find.text('Start camera mode'), findsOneWidget);
    });

    testWidgets('onCameraTab_withBlankName_showsErrorAndStaysOnTab', (
      tester,
    ) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
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
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );
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
      expect(find.text('Paired with ReCam'), findsOneWidget);
      expect(find.textContaining('owner'), findsNothing);
      expect(find.text('Add camera'), findsOneWidget);
    });

    testWidgets('resetApp_afterConfirming_forgetsBothPairings', (tester) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );
      await openApp(tester);
      await tester.tap(find.byTooltip('Show menu'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Reset app'));
      await tester.pumpAndSettle();

      // act
      await tester.tap(find.widgetWithText(FilledButton, 'Reset'));
      await tester.pumpAndSettle();

      // assert
      expect(store.sessions, isEmpty);
      expect(find.text('This phone will be:'), findsOneWidget);
    });

    testWidgets('resetApp_whenCancelled_keepsThePairings', (tester) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      await openApp(tester);
      await tester.tap(find.byTooltip('Show menu'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Reset app'));
      await tester.pumpAndSettle();

      // act
      await tester.tap(find.text('Cancel'));
      await tester.pumpAndSettle();

      // assert
      expect(store.sessions, isNotEmpty);
    });
  });

  group('RecamApp first run', () {
    testWidgets('withNothingPaired_asksWhatThePhoneIsFor', (tester) async {
      // arrange
      // (empty store)

      // act
      await openApp(tester);

      // assert
      expect(find.text('This phone will be:'), findsOneWidget);
      expect(find.text('A camera'), findsOneWidget);
      expect(find.text('For watching'), findsOneWidget);
      expect(find.byType(NavigationBar), findsNothing);
    });

    testWidgets('forWatching_readsCodeAndShowsTheCameraList', (tester) async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult());
      codesToRead.add(pairingQr(role: 'owner'));
      await openApp(tester);

      // act
      await tester.tap(find.text('For watching'));
      await tester.pumpAndSettle();

      // assert
      expect(codesToRead, isEmpty);
      expect(api.pairCalls.single.name, 'Phone that watches');
      expect(find.text('This phone will be:'), findsNothing);
      expect(find.text('Paired with ReCam'), findsOneWidget);
      expect(find.byType(NavigationBar), findsOneWidget);
    });

    testWidgets('asCamera_readsCodeAndOpensCameraMode', (tester) async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult(role: DeviceRole.camera));
      codesToRead.add(pairingQr(role: 'camera'));
      await openApp(tester);
      await tester.enterText(find.byType(TextField), 'Garage');

      // act
      await tester.tap(find.text('A camera'));
      await tester.pumpAndSettle();

      // assert
      expect(api.pairCalls.single.name, 'Garage');
      expect(find.byType(CameraModeScreen), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('whenReaderIsClosed_staysOnFirstRun', (tester) async {
      // arrange
      codesToRead.add(null);
      await openApp(tester);

      // act
      await tester.tap(find.text('For watching'));
      await tester.pumpAndSettle();

      // assert
      expect(api.pairCalls, isEmpty);
      expect(find.text('This phone will be:'), findsOneWidget);
    });

    testWidgets('whenCodeIsNotReCam_showsTheErrorOnFirstRun', (tester) async {
      // arrange
      codesToRead.add('hello');
      await openApp(tester);

      // act
      await tester.tap(find.text('For watching'));
      await tester.pumpAndSettle();

      // assert
      expect(find.text('This phone will be:'), findsOneWidget);
      expect(find.text('This is not a ReCam pairing QR code.'), findsOneWidget);
    });
  });
}
