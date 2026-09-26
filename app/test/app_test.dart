import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/app.dart';
import 'package:recam/camera/battery_guide.dart';
import 'package:recam/camera/camera_mode_controller.dart';
import 'package:recam/camera/camera_mode_screen.dart';
import 'package:recam/camera/camera_pairing_controller.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/pinned_http_overrides.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/core/pairing/pairing_service.dart';
import 'package:recam/core/storage/credential_store.dart';
import 'package:recam/core/network/hub_session.dart';
import 'package:recam/viewer/add_device_screen.dart';
import 'package:recam/viewer/camera_list_controller.dart';
import 'package:recam/viewer/recording_timeline_controller.dart';
import 'package:recam/viewer/recordings_timeline_screen.dart';
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
        batteryGuide: BatteryGuideController(
          optimization: FakeBatteryOptimization(),
        ),
        cameraMode: (_) => CameraModeController(
          hub: HubSession(client: FakeHubClient(), delay: (_) async {}),
          battery: FakeBatteryReader(),
          screen: FakeScreenController(),
          keepAlive: FakeKeepAlive(),
          publisher: FakePublisher(),
          capture: FakeCapture(),
        ),
        cameraList: (session) => CameraListController(
          api: api,
          session: session,
          hub: HubSession(client: FakeHubClient(), delay: (_) async {}),
          viewerFactory: (_) => FakeViewer(),
          timelineFactory: (cameraId) => RecordingTimelineController(
            api: FakeApiClient(),
            session: pairedSession(),
            cameraId: cameraId,
            player: FakeRecordingPlayer(),
            segments: FakeSegmentSource(),
          ),
          adjustments: FakeAdjustmentStore(),
        ),
        links: links,
        readCode: (_) async => codesToRead.removeAt(0),
        ready: Future.value(),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('RecamApp pairing links', () {
    testWidgets('withMonitorLink_opensWatchTabAndPairs', (tester) async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult());
      await openApp(tester);

      // act
      links.controller.add(Uri.parse(pairingQr(role: 'viewer')));
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
      links.controller.add(Uri.parse(pairingQr(role: 'viewer')));
      await tester.pumpAndSettle();

      // assert
      expect(api.pairCalls, isEmpty);
      expect(find.text('Paired with ReCam'), findsOneWidget);
    });
  });

  group('RecamApp', () {
    testWidgets('onStart_asMonitor_showsTheCameraListAndTheMonitorTabs', (
      tester,
    ) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());

      // act
      await openApp(tester);

      // assert
      expect(find.text('ReCam · Monitor'), findsOneWidget);
      expect(find.text('Paired with ReCam'), findsOneWidget);
      expect(find.text('Add camera'), findsOneWidget);
      expect(
        tester
            .widgetList<NavigationDestination>(
              find.byType(NavigationDestination),
            )
            .map((destination) => destination.label),
        ['Cameras', 'Recordings', 'Devices', 'Settings'],
      );
    });

    testWidgets('recordingsTab_showsTheRecordingCameraAndSwitchesCamera', (
      tester,
    ) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      const garage = CameraInfo(
        id: 'garage',
        name: 'Garage',
        online: true,
        publishing: false,
      );
      const porch = CameraInfo(
        id: 'porch',
        name: 'Porch',
        online: true,
        publishing: false,
        recording: true,
      );
      // The list loads on start and again when the hub connects.
      api.cameraResults.addAll([
        ApiSuccess(const [garage, porch]),
        ApiSuccess(const [garage, porch]),
      ]);
      await openApp(tester);
      await tester.tap(find.text('Recordings'));
      await tester.pumpAndSettle();
      final first = tester
          .widget<RecordingsTimelinePane>(find.byType(RecordingsTimelinePane))
          .key;

      // act
      await tester.tap(find.text('Porch · Recording'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Garage').last);
      await tester.pumpAndSettle();

      // assert
      expect(first, const ValueKey('porch'));
      expect(
        tester
            .widget<RecordingsTimelinePane>(find.byType(RecordingsTimelinePane))
            .key,
        const ValueKey('garage'),
      );
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('onStart_asCamera_showsStartCameraModeWithoutTabs', (
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
      expect(find.text('ReCam · Camera'), findsOneWidget);
      expect(find.text('Start camera mode'), findsOneWidget);
      expect(find.byType(NavigationBar), findsNothing);
    });

    testWidgets('onStart_pairedInBothRoles_keepsTheMonitor', (tester) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );

      // act
      await openApp(tester);

      // assert
      expect(api.leaveCalls, hasLength(1));
      expect(await store.read(PairingSlot.camera), isNull);
      expect(await store.read(PairingSlot.viewer), isNotNull);
      expect(find.text('ReCam · Monitor'), findsOneWidget);
    });

    testWidgets('scanMonitorCodeOnCamera_afterConfirming_becomesMonitor', (
      tester,
    ) async {
      // arrange
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult(role: DeviceRole.viewer));
      codesToRead.add(pairingQr(role: 'viewer'));
      await openApp(tester);
      await tester.tap(find.byTooltip('Show menu'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Scan QR code'));
      await tester.pumpAndSettle();
      final asked = find
          .text('This phone is a camera. Make it a Monitor?')
          .evaluate()
          .length;

      // act
      await tester.tap(find.widgetWithText(FilledButton, 'Switch'));
      await tester.pumpAndSettle();

      // assert
      expect(asked, 1);
      expect(api.leaveCalls, hasLength(1));
      expect(api.pairCalls.single.name, 'Monitor');
      expect(await store.read(PairingSlot.camera), isNull);
      expect(find.text('ReCam · Monitor'), findsOneWidget);
    });

    testWidgets('scanMonitorCodeOnCamera_whenCancelled_staysACamera', (
      tester,
    ) async {
      // arrange
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );
      codesToRead.add(pairingQr(role: 'viewer'));
      await openApp(tester);
      await tester.tap(find.byTooltip('Show menu'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Scan QR code'));
      await tester.pumpAndSettle();

      // act
      await tester.tap(find.text('Cancel'));
      await tester.pumpAndSettle();

      // assert
      expect(api.leaveCalls, isEmpty);
      expect(api.pairCalls, isEmpty);
      expect(find.text('ReCam · Camera'), findsOneWidget);
    });

    testWidgets('scanCameraCodeOnMonitor_afterConfirming_asksNameAndFilms', (
      tester,
    ) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult(role: DeviceRole.camera));
      codesToRead.add(pairingQr(role: 'camera'));
      await openApp(tester);
      await tester.tap(find.byTooltip('Show menu'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Scan QR code'));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(FilledButton, 'Switch'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField), 'Garage');

      // act
      await tester.tap(find.text('Confirm'));
      await tester.pumpAndSettle();

      // assert
      expect(api.leaveCalls, hasLength(1));
      expect(api.pairCalls.single.name, 'Garage');
      expect(find.byType(CameraModeScreen), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('resetApp_afterConfirming_forgetsThePairing', (tester) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
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
      expect(find.text('Welcome to ReCam'), findsOneWidget);
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
    testWidgets('withNothingPaired_offersScanQrCode', (tester) async {
      // arrange
      // (empty store)

      // act
      await openApp(tester);

      // assert
      expect(find.text('Welcome to ReCam'), findsOneWidget);
      expect(find.text('Scan QR code'), findsOneWidget);
      expect(find.byType(NavigationBar), findsNothing);
    });

    testWidgets('monitorCode_pairsAsMonitorAndShowsTheCameraList', (
      tester,
    ) async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult(role: DeviceRole.viewer));
      codesToRead.add(pairingQr(role: 'viewer'));
      await openApp(tester);

      // act
      await tester.tap(find.text('Scan QR code'));
      await tester.pumpAndSettle();

      // assert
      expect(codesToRead, isEmpty);
      expect(api.pairCalls.single.name, 'Monitor');
      expect(find.text('Welcome to ReCam'), findsNothing);
      expect(find.text('Paired with ReCam'), findsOneWidget);
      expect(find.byType(NavigationBar), findsOneWidget);
    });

    testWidgets('cameraCode_asksNameAndOpensCameraMode', (tester) async {
      // arrange
      api
        ..healthyHosts = {'192.168.0.10'}
        ..pairResult = ApiSuccess(pairResult(role: DeviceRole.camera));
      codesToRead.add(pairingQr(role: 'camera'));
      await openApp(tester);
      await tester.tap(find.text('Scan QR code'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField), 'Garage');

      // act
      await tester.tap(find.text('Confirm'));
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
      await tester.tap(find.text('Scan QR code'));
      await tester.pumpAndSettle();

      // assert
      expect(api.pairCalls, isEmpty);
      expect(find.text('Welcome to ReCam'), findsOneWidget);
    });

    testWidgets('whenCodeIsNotReCam_showsTheErrorOnFirstRun', (tester) async {
      // arrange
      codesToRead.add('hello');
      await openApp(tester);

      // act
      await tester.tap(find.text('Scan QR code'));
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Welcome to ReCam'), findsOneWidget);
      expect(find.text('This is not a ReCam pairing QR code.'), findsOneWidget);
    });
  });

  group('RecamApp adding devices', () {
    PairingTokenResult token(String qrUri) => PairingTokenResult(
      id: 'token-1',
      qrUri: qrUri,
      validFor: const Duration(minutes: 10),
    );

    testWidgets('plusOnMonitor_opensTheCameraQrRightAway', (tester) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      api.tokenResults.add(ApiSuccess(token('recam://pair?r=camera')));
      await openApp(tester);

      // act
      await tester.tap(find.byTooltip('Add camera'));
      await tester.pumpAndSettle();

      // assert
      expect(api.tokenRoles, [DeviceRole.camera]);
      expect(find.byType(AddDeviceScreen), findsOneWidget);
      expect(find.byType(SegmentedButton<DeviceRole>), findsNothing);
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('whenTheCameraPairs_closesTheQrAndReloadsTheList', (
      tester,
    ) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      api
        ..tokenResults.add(ApiSuccess(token('recam://pair?r=camera')))
        ..tokenUsedResults.add(ApiSuccess(true));
      await openApp(tester);
      await tester.tap(find.byTooltip('Add camera'));
      await tester.pumpAndSettle();
      final loadsBefore = api.cameraCalls;

      // act
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();

      // assert
      expect(find.byType(AddDeviceScreen), findsNothing);
      expect(api.cameraCalls, loadsBefore + 1);
    });

    testWidgets('firstCamera_asksForTheSpaceAndSavesIt', (tester) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      api
        ..quotaResult = ApiSuccess(
          const RecordingQuota(
            megabytes: 2048,
            usedBytes: 0,
            freeBytes: 10 * 1024 * 1024 * 1024,
          ),
        )
        ..tokenResults.add(ApiSuccess(token('recam://pair?r=camera')))
        ..tokenUsedResults.add(ApiSuccess(true));
      await openApp(tester);
      await tester.tap(find.byTooltip('Add camera'));
      await tester.pumpAndSettle();
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();
      final asked = find.text('Your first camera is ready').evaluate().length;

      // act
      await tester.tap(find.text('Save and continue'));
      await tester.pumpAndSettle();

      // assert
      expect(asked, 1);
      expect(api.quotaChanges, [2048]);
      expect(find.text('Your first camera is ready'), findsNothing);
    });

    testWidgets('secondCamera_doesNotAskForTheSpace', (tester) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      const porch = CameraInfo(
        id: 'cam',
        name: 'Porch',
        online: true,
        publishing: false,
      );
      api
        ..cameraResults.addAll([
          ApiSuccess(const [porch]),
          ApiSuccess(const [porch]),
        ])
        ..tokenResults.add(ApiSuccess(token('recam://pair?r=camera')))
        ..tokenUsedResults.add(ApiSuccess(true));
      await openApp(tester);
      await tester.tap(find.byTooltip('Add camera'));
      await tester.pumpAndSettle();

      // act
      await tester.pump(const Duration(seconds: 2));
      await tester.pumpAndSettle();

      // assert
      expect(find.byType(AddDeviceScreen), findsNothing);
      expect(find.text('Your first camera is ready'), findsNothing);
    });

    testWidgets('menuAddMonitor_opensTheMonitorQr', (tester) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      api.tokenResults.add(ApiSuccess(token('recam://pair?r=viewer')));
      await openApp(tester);
      await tester.tap(find.byTooltip('Show menu'));
      await tester.pumpAndSettle();

      // act
      await tester.tap(find.text('Add Monitor'));
      await tester.pumpAndSettle();

      // assert
      expect(api.tokenRoles, [DeviceRole.viewer]);
      expect(
        find.textContaining('It becomes a Monitor of these cameras.'),
        findsOneWidget,
      );
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('settingsTab_showsTheSharedSpaceSlider', (tester) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      api.quotaResult = ApiSuccess(
        const RecordingQuota(
          megabytes: 2048,
          usedBytes: 0,
          freeBytes: 10 * 1024 * 1024 * 1024,
        ),
      );
      await openApp(tester);

      // act
      await tester.tap(find.text('Settings'));
      await tester.pumpAndSettle();

      // assert
      expect(
        find.text(
          'This is the total for all cameras together. The recordings stay '
          'on the server, not on the phones.',
        ),
        findsOneWidget,
      );
      expect(find.byType(Slider), findsOneWidget);
      expect(find.text('Space for recordings: 2 GB'), findsOneWidget);
      expect(find.text('About 6.8 hours of one camera fit.'), findsOneWidget);
    });

    testWidgets('devicesTab_removesACameraAfterConfirming', (tester) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      const camera = DeviceInfo(
        id: 'cam',
        name: 'Samsung A10',
        role: DeviceRole.camera,
        online: true,
      );
      api.deviceResults.addAll([
        ApiSuccess(const [camera]),
        ApiSuccess(const <DeviceInfo>[]),
      ]);
      await openApp(tester);
      await tester.tap(find.text('Devices'));
      await tester.pumpAndSettle();

      // act
      await tester.tap(find.byTooltip('Remove'));
      await tester.pumpAndSettle();
      await tester.tap(find.widgetWithText(FilledButton, 'Remove'));
      await tester.pumpAndSettle();

      // assert
      expect(api.removedDevices, ['cam']);
      expect(find.text('Samsung A10'), findsNothing);
    });

    testWidgets('menuWithoutMonitor_hasNoAddMonitor', (tester) async {
      // arrange
      await store.write(
        PairingSlot.camera,
        pairedSession(role: DeviceRole.camera),
      );
      await openApp(tester);

      // act
      await tester.tap(find.byTooltip('Show menu'));
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Add Monitor'), findsNothing);
      expect(find.text('Reset app'), findsOneWidget);
    });
  });
}
