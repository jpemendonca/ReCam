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

  Future<void> openScreen(WidgetTester tester) async {
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
        ),
      ),
    );
    await tester.pump();
  }

  group('AddDeviceScreen', () {
    testWidgets('onOpen_showsACameraQr', (tester) async {
      // arrange
      api.tokenResults.add(ApiSuccess(_token('recam://pair?r=camera')));

      // act
      await openScreen(tester);

      // assert
      expect(api.tokenRoles, [DeviceRole.camera]);
      expect(
        find.text('On the camera phone, open ReCam and scan this QR code.'),
        findsOneWidget,
      );
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('whenChoosingAnotherPhoneToWatch_showsAViewerQr', (
      tester,
    ) async {
      // arrange
      api.tokenResults.addAll([
        ApiSuccess(_token('recam://pair?r=camera')),
        ApiSuccess(_token('recam://pair?r=viewer')),
      ]);
      await openScreen(tester);

      // act
      await tester.tap(find.text('Another phone to watch'));
      await tester.pump();

      // assert
      expect(api.tokenRoles, [DeviceRole.camera, DeviceRole.viewer]);
      expect(find.textContaining('It will watch these cameras too.'), findsOne);
      expect(find.text('Copy code'), findsOneWidget);
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
