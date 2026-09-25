import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/l10n/generated/app_localizations.dart';
import 'package:recam/viewer/add_camera_screen.dart';

import '../support/fakes.dart';

void main() {
  group('AddCameraScreen', () {
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
      final api = FakeApiClient()
        ..tokenResults.add(
          ApiSuccess(
            PairingTokenResult(
              qrUri: 'recam://pair?v=1&t=abc',
              validFor: const Duration(minutes: 10),
            ),
          ),
        );
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
          ],
          supportedLocales: AppLocalizations.supportedLocales,
          home: AddCameraScreen(api: api, session: pairedSession()),
        ),
      );
      await tester.pump();

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
