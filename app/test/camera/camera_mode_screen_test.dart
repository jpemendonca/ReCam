import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/camera/camera_mode_controller.dart';
import 'package:recam/camera/camera_mode_screen.dart';
import 'package:recam/core/device/battery_reader.dart';
import 'package:recam/core/network/hub_client.dart';
import 'package:recam/core/network/hub_session.dart';
import 'package:recam/l10n/generated/app_localizations.dart';

import '../support/fakes.dart';

void main() {
  group('CameraModeScreen', () {
    testWidgets('onceConnected_showsStatusAndBatteryWithoutTapping', (
      tester,
    ) async {
      // arrange
      final battery = FakeBatteryReader()
        ..reading = const BatteryReading(level: 57, isCharging: false);
      CameraModeController create() => CameraModeController(
        hub: HubSession(client: FakeHubClient(), delay: (_) async {}),
        battery: battery,
        screen: FakeScreenController(),
        keepAlive: FakeKeepAlive(),
        publisher: FakePublisher(),
        capture: FakeCapture(),
        network: FakeNetworkStatus(),
      );

      // act
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
          ],
          supportedLocales: AppLocalizations.supportedLocales,
          home: CameraModeScreen(create: create, onPairingLost: () {}),
        ),
      );
      await tester.runAsync(settle);
      await tester.pump();

      // assert
      expect(find.text('Connected. This phone is a camera.'), findsOneWidget);
      expect(find.text('Nobody is watching.'), findsOneWidget);
      expect(find.text('Battery 57%'), findsOneWidget);
      expect(find.text('Stop camera'), findsOneWidget);
      expect(find.text('This phone: dev'), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('whenViewersWatch_showsHowMany', (tester) async {
      // arrange
      final client = FakeHubClient();
      CameraModeController create() => CameraModeController(
        hub: HubSession(client: client, delay: (_) async {}),
        battery: FakeBatteryReader(),
        screen: FakeScreenController(),
        keepAlive: FakeKeepAlive(),
        publisher: FakePublisher(),
        capture: FakeCapture(),
        network: FakeNetworkStatus(),
      );
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
          ],
          supportedLocales: AppLocalizations.supportedLocales,
          home: CameraModeScreen(create: create, onPairingLost: () {}),
        ),
      );
      await tester.runAsync(settle);

      // act
      client.receive('WatchersChanged', [3]);
      await tester.pump();

      // assert
      expect(find.text('3 people are watching.'), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('whenServerRejectsPairing_leavesAndReportsIt', (tester) async {
      // arrange
      final client = FakeHubClient()
        ..connectResults.add(HubConnectOutcome.rejected);
      var lost = 0;
      CameraModeController create() => CameraModeController(
        hub: HubSession(client: client, delay: (_) async {}),
        battery: FakeBatteryReader(),
        screen: FakeScreenController(),
        keepAlive: FakeKeepAlive(),
        publisher: FakePublisher(),
        capture: FakeCapture(),
        network: FakeNetworkStatus(),
      );
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
          ],
          supportedLocales: AppLocalizations.supportedLocales,
          home: Builder(
            builder: (context) => TextButton(
              onPressed: () => Navigator.of(context).push<void>(
                MaterialPageRoute(
                  builder: (_) => CameraModeScreen(
                    create: create,
                    onPairingLost: () => lost++,
                  ),
                ),
              ),
              child: const Text('open'),
            ),
          ),
        ),
      );

      // act
      await tester.tap(find.text('open'));
      await tester.pump();
      await tester.runAsync(settle);
      await tester.pumpAndSettle();

      // assert
      expect(lost, 1);
      expect(find.byType(CameraModeScreen), findsNothing);
    });

    testWidgets('showImage_opensAndHidesTheThumbnail', (tester) async {
      // arrange
      final capture = FakeCapture();
      CameraModeController create() => CameraModeController(
        hub: HubSession(client: FakeHubClient(), delay: (_) async {}),
        battery: FakeBatteryReader(),
        screen: FakeScreenController(),
        keepAlive: FakeKeepAlive(),
        publisher: FakePublisher(),
        capture: capture,
        network: FakeNetworkStatus(),
      );
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: CameraModeScreen(create: create, onPairingLost: () {}),
        ),
      );
      await tester.runAsync(settle);
      await tester.pump();
      final closedAtStart = find
          .byKey(const Key('camera-preview'))
          .evaluate()
          .length;

      // act
      await tester.tap(find.text('Show image'));
      await tester.runAsync(settle);
      await tester.pump();
      final shownCount = find
          .byKey(const Key('camera-preview'))
          .evaluate()
          .length;
      await tester.tap(find.text('Hide image'));
      await tester.runAsync(settle);
      await tester.pump();

      // assert
      expect(closedAtStart, 0);
      expect(shownCount, 1);
      expect(find.byKey(const Key('camera-preview')), findsNothing);
      expect(find.text('Show image'), findsOneWidget);
      expect(capture.isOpen, isFalse);
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('withoutNetwork_saysSoAndTryAgainConnects', (tester) async {
      // arrange
      final network = FakeNetworkStatus()..online = false;
      var lost = 0;
      CameraModeController create() => CameraModeController(
        hub: HubSession(client: FakeHubClient(), delay: (_) async {}),
        battery: FakeBatteryReader(),
        screen: FakeScreenController(),
        keepAlive: FakeKeepAlive(),
        publisher: FakePublisher(),
        capture: FakeCapture(),
        network: network,
      );
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: CameraModeScreen(create: create, onPairingLost: () => lost++),
        ),
      );
      await tester.runAsync(settle);
      await tester.pump();
      final shownOffline = find
          .text('No connection. Turn on Wi-Fi and try again.')
          .evaluate()
          .length;
      final backShown = find.text('Back').evaluate().length;

      // act
      network.online = true;
      await tester.tap(find.text('Try again'));
      await tester.runAsync(settle);
      await tester.pump();

      // assert
      expect(shownOffline, 1);
      expect(backShown, 1);
      expect(lost, 0);
      expect(find.text('Connected. This phone is a camera.'), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('whenServerDoesNotAnswerInTime_explainsAndKeepsThePairing', (
      tester,
    ) async {
      // arrange
      final client = FakeHubClient()
        ..connectResults.add(HubConnectOutcome.unreachable);
      var lost = 0;
      CameraModeController create() => CameraModeController(
        hub: HubSession(client: client, delay: (_) => Completer<void>().future),
        battery: FakeBatteryReader(),
        screen: FakeScreenController(),
        keepAlive: FakeKeepAlive(),
        publisher: FakePublisher(),
        capture: FakeCapture(),
        network: FakeNetworkStatus(),
      );
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: CameraModeScreen(create: create, onPairingLost: () => lost++),
        ),
      );
      await tester.pump();
      final connectingFirst = find
          .text('Connecting to the server…')
          .evaluate()
          .length;

      // act
      await tester.pump(const Duration(seconds: 30));

      // assert
      expect(connectingFirst, 1);
      expect(
        find.textContaining('Could not reach the server.'),
        findsOneWidget,
      );
      expect(find.text('Try again'), findsOneWidget);
      expect(lost, 0);
      await tester.pumpWidget(const SizedBox());
    });
  });
}
