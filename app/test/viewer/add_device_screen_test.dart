import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/l10n/generated/app_localizations.dart';
import 'package:recam/viewer/add_device_screen.dart';

import '../support/fakes.dart';

PairingTokenResult _token(String qrUri) =>
    PairingTokenResult(qrUri: qrUri, validFor: const Duration(minutes: 10));

void main() {
  late FakeApiClient api;

  setUp(() => api = FakeApiClient());

  Future<void> openScreen(
    WidgetTester tester, {
    DeviceRole role = DeviceRole.camera,
  }) async {
    await tester.pumpWidget(
      MaterialApp(
        localizationsDelegates: const [
          AppLocalizations.delegate,
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
        ],
        supportedLocales: AppLocalizations.supportedLocales,
        home: AddDeviceScreen(
          api: api,
          session: pairedSession(role: DeviceRole.viewer),
          role: role,
        ),
      ),
    );
    await tester.pump();
  }

  group('AddDeviceScreen', () {
    testWidgets('forCamera_showsTheCameraQrWithoutAChoice', (tester) async {
      // arrange
      api.tokenResults.add(ApiSuccess(_token('recam://pair?r=camera')));

      // act
      await openScreen(tester);

      // assert
      expect(api.tokenRoles, [DeviceRole.camera]);
      expect(find.text('Add camera'), findsOneWidget);
      expect(
        find.text(
          'On the phone that will film, open ReCam, choose Film and scan this QR code.',
        ),
        findsOneWidget,
      );
      expect(find.byType(SegmentedButton<DeviceRole>), findsNothing);
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('forMonitor_showsTheMonitorQr', (tester) async {
      // arrange
      api.tokenResults.add(ApiSuccess(_token('recam://pair?r=viewer')));

      // act
      await openScreen(tester, role: DeviceRole.viewer);

      // assert
      expect(api.tokenRoles, [DeviceRole.viewer]);
      expect(find.text('Add Monitor'), findsOneWidget);
      expect(
        find.textContaining('It becomes a Monitor of these cameras.'),
        findsOneWidget,
      );
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('whenTappingCopyCode_putsPairingUriInClipboard', (
      tester,
    ) async {
      // arrange
      String? clipboardText;
      tester.binding.defaultBinaryMessenger.setMockMethodCallHandler(
        SystemChannels.platform,
        (call) async {
          if (call.method == 'Clipboard.setData') {
            clipboardText = (call.arguments as Map)['text'] as String?;
          }
          return null;
        },
      );
      api.tokenResults.add(ApiSuccess(_token('recam://pair?v=1&t=abc')));
      await openScreen(tester);

      // act
      await tester.tap(find.text('Copy code'));
      await tester.pump();

      // assert
      expect(clipboardText, 'recam://pair?v=1&t=abc');
      expect(find.text('Pairing code copied.'), findsOneWidget);
      await tester.pumpWidget(const SizedBox());
    });
  });
}
