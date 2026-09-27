import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/l10n/generated/app_localizations.dart';
import 'package:recam/viewer/devices_view.dart';

import '../support/fakes.dart';

void main() {
  group('DevicesView', () {
    testWidgets('whenTheServerSaysTheListChanged_showsTheNewMonitor', (
      tester,
    ) async {
      // arrange
      const camera = DeviceInfo(
        id: 'cam',
        name: 'Samsung A10',
        role: DeviceRole.camera,
        online: true,
      );
      final api = FakeApiClient()
        ..deviceResults.add(ApiSuccess(const [camera]));
      final changes = ValueNotifier(0);
      await tester.pumpWidget(
        MaterialApp(
          locale: const Locale('en'),
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: Scaffold(
            body: DevicesView(
              api: api,
              session: pairedSession(),
              changes: changes,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      final before = find.text('Redmi 6A').evaluate().length;
      api.deviceResults.add(
        ApiSuccess([
          camera,
          DeviceInfo(
            id: 'monitor',
            name: 'Redmi 6A',
            role: DeviceRole.viewer,
            online: false,
            lastSeenAt: DateTime.utc(2026, 9, 27, 13, 5),
          ),
        ]),
      );

      // act
      changes.value++;
      await tester.pumpAndSettle();

      // assert
      expect(before, 0);
      expect(find.text('Redmi 6A'), findsOneWidget);
      expect(
        find.textContaining('Offline · last seen 9/27/2026'),
        findsOneWidget,
      );
    });
  });
}
