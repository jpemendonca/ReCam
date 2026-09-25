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
  });
}
