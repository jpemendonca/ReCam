import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/language/language_controller.dart';
import 'package:recam/core/language/language_picker.dart';
import 'package:recam/l10n/generated/app_localizations.dart';

import '../../support/fakes.dart';

void main() {
  group('LanguagePicker', () {
    testWidgets('choosing_switchesTheTextRightAwayAndRemembersIt', (
      tester,
    ) async {
      // arrange
      final store = MemoryLanguageStore();
      final controller = LanguageController(store);
      await tester.pumpWidget(
        LanguageScope(
          controller: controller,
          child: ListenableBuilder(
            listenable: controller,
            builder: (context, _) => MaterialApp(
              locale: controller.language.locale,
              localizationsDelegates: AppLocalizations.localizationsDelegates,
              supportedLocales: AppLocalizations.supportedLocales,
              home: const Scaffold(body: LanguagePicker()),
            ),
          ),
        ),
      );
      final before = find.text('Language').evaluate().length;

      // act
      await tester.tap(find.text('Português'));
      await tester.pumpAndSettle();

      // assert
      expect(before, 1);
      expect(find.text('Idioma'), findsOneWidget);
      expect(find.text('Do celular'), findsOneWidget);
      expect(store.saved, AppLanguage.portuguese);
    });

    test('device_isForgottenSoThePhoneDecidesAgain', () async {
      // arrange
      final store = MemoryLanguageStore()..saved = AppLanguage.english;
      final controller = LanguageController(store);
      await controller.load();

      // act
      await controller.choose(AppLanguage.device);

      // assert
      expect(controller.language.locale, isNull);
      expect(store.saved, AppLanguage.device);
    });
  });
}
