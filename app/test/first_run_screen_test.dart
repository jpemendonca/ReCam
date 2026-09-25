import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/first_run_screen.dart';
import 'package:recam/l10n/generated/app_localizations.dart';

void main() {
  Future<void> open(
    WidgetTester tester, {
    required Locale locale,
    Future<void> Function(String name)? onCamera,
    Future<void> Function()? onWatch,
  }) async {
    await tester.pumpWidget(
      MaterialApp(
        locale: locale,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: Scaffold(
          body: FirstRunScreen(
            onCamera: onCamera ?? (_) async {},
            onWatch: onWatch ?? () async {},
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  group('FirstRunScreen', () {
    testWidgets('inPortuguese_offersFilmarAndAssistir', (tester) async {
      // arrange
      // (Portuguese locale)

      // act
      await open(tester, locale: const Locale('pt'));

      // assert
      expect(find.text('Este celular vai ser usado para:'), findsOneWidget);
      expect(find.widgetWithText(FilledButton, 'Filmar'), findsOneWidget);
      expect(find.widgetWithText(FilledButton, 'Assistir'), findsOneWidget);
      expect(find.byIcon(Icons.videocam), findsOneWidget);
      expect(find.byIcon(Icons.live_tv), findsOneWidget);
      expect(find.widgetWithText(TextField, 'Câmera'), findsOneWidget);
    });

    testWidgets('inEnglish_offersFilmAndWatch', (tester) async {
      // arrange
      // (English locale)

      // act
      await open(tester, locale: const Locale('en'));

      // assert
      expect(find.text('This phone will be used to:'), findsOneWidget);
      expect(find.widgetWithText(FilledButton, 'Film'), findsOneWidget);
      expect(find.widgetWithText(FilledButton, 'Watch'), findsOneWidget);
    });

    testWidgets('filmar_passesTheCameraName', (tester) async {
      // arrange
      String? name;
      await open(
        tester,
        locale: const Locale('pt'),
        onCamera: (value) async => name = value,
      );
      await tester.enterText(find.byType(TextField), 'Varanda');

      // act
      await tester.tap(find.text('Filmar'));
      await tester.pump();

      // assert
      expect(name, 'Varanda');
    });
  });
}
