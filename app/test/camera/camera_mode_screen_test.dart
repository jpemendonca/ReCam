import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/camera/camera_mode_controller.dart';
import 'package:recam/camera/camera_mode_screen.dart';
import 'package:recam/core/device/battery_reader.dart';
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
          home: CameraModeScreen(create: create),
        ),
      );
      await tester.runAsync(settle);
      await tester.pump();

      // assert
      expect(find.text('Connected. This phone is a camera.'), findsOneWidget);
      expect(find.text('Battery 57%'), findsOneWidget);
      expect(find.text('Stop camera'), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    });
  });
}
