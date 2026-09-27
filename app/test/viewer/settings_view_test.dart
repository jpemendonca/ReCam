import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/language/language_controller.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/l10n/generated/app_localizations.dart';
import 'package:recam/viewer/settings_view.dart';

import '../support/fakes.dart';

void main() {
  late FakeApiClient api;

  setUp(() {
    api = FakeApiClient()
      ..quotaResult = ApiSuccess(
        const RecordingQuota(
          megabytes: 2048,
          usedBytes: 0,
          freeBytes: 9500 * RecordingQuota.bytesPerMegabyte,
        ),
      );
  });

  Future<void> show(WidgetTester tester, {VoidCallback? onDone}) async {
    await tester.pumpWidget(
      LanguageScope(
        controller: LanguageController(MemoryLanguageStore()),
        child: MaterialApp(
          locale: const Locale('pt'),
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: Scaffold(
            body: SettingsView(
              api: api,
              session: pairedSession(),
              recordingCameras: 1,
              onDone: onDone,
            ),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('SettingsView', () {
    testWidgets('afterTheFirstCamera_saysItIsAlreadyRecording', (tester) async {
      // act
      await show(tester, onDone: () {});

      // assert
      expect(find.textContaining('A câmera já está gravando.'), findsOneWidget);
      expect(find.byKey(const Key('language-picker')), findsNothing);
    });

    testWidgets('asTheSettingsTab_showsTheLanguageChoice', (tester) async {
      // act
      await show(tester);
      await tester.scrollUntilVisible(
        find.byKey(const Key('language-picker')),
        200,
        scrollable: find.byType(Scrollable).first,
      );

      // assert
      expect(find.byKey(const Key('language-picker')), findsOneWidget);
      expect(find.textContaining('A câmera já está gravando.'), findsNothing);
    });
  });
}
