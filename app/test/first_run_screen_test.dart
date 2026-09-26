import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/first_run_screen.dart';
import 'package:recam/l10n/generated/app_localizations.dart';

import 'support/fakes.dart';

void main() {
  late List<String?> codes;
  late List<(String, String?)> paired;

  Future<void> open(WidgetTester tester, {required Locale locale}) async {
    await tester.pumpWidget(
      MaterialApp(
        locale: locale,
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: Scaffold(
          body: FirstRunScreen(
            readCode: () async => codes.removeAt(0),
            onPair: (code, name) async => paired.add((code, name)),
          ),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  setUp(() {
    codes = [];
    paired = [];
  });

  group('FirstRunScreen', () {
    testWidgets('inPortuguese_offersOnlyLerQrCode', (tester) async {
      // act
      await open(tester, locale: const Locale('pt'));

      // assert
      expect(find.text('Boas-vindas ao ReCam'), findsOneWidget);
      expect(find.widgetWithText(FilledButton, 'Ler QR code'), findsOneWidget);
      expect(find.byType(FilledButton), findsOneWidget);
      expect(find.byType(TextField), findsNothing);
      expect(find.textContaining('Adicionar câmera'), findsOneWidget);
    });

    testWidgets('inEnglish_offersOnlyScanQrCode', (tester) async {
      // act
      await open(tester, locale: const Locale('en'));

      // assert
      expect(find.text('Welcome to ReCam'), findsOneWidget);
      expect(find.widgetWithText(FilledButton, 'Scan QR code'), findsOneWidget);
    });

    testWidgets('cameraCode_asksTheNameThenPairs', (tester) async {
      // arrange
      final code = pairingQr(role: 'camera');
      codes.add(code);
      await open(tester, locale: const Locale('pt'));
      await tester.tap(find.text('Ler QR code'));
      await tester.pumpAndSettle();
      final asked = find.text('Dê um nome a esta câmera').evaluate().length;
      await tester.enterText(find.byType(TextField), 'Varanda');

      // act
      await tester.tap(find.text('Confirmar'));
      await tester.pump();

      // assert
      expect(asked, 1);
      expect(paired, [(code, 'Varanda')]);
    });

    testWidgets('monitorCode_pairsRightAwayWithoutName', (tester) async {
      // arrange
      final code = pairingQr(role: 'viewer');
      codes.add(code);
      await open(tester, locale: const Locale('pt'));

      // act
      await tester.tap(find.text('Ler QR code'));
      await tester.pumpAndSettle();

      // assert
      expect(paired, [(code, null)]);
      expect(find.byType(TextField), findsNothing);
    });

    testWidgets('notReCamCode_saysSoAndPairsNothing', (tester) async {
      // arrange
      codes.add('hello');
      await open(tester, locale: const Locale('pt'));

      // act
      await tester.tap(find.text('Ler QR code'));
      await tester.pumpAndSettle();

      // assert
      expect(paired, isEmpty);
      expect(
        find.text('Este não é um QR code de pareamento do ReCam.'),
        findsOneWidget,
      );
    });

    testWidgets('emptyName_isRefused', (tester) async {
      // arrange
      codes.add(pairingQr(role: 'camera'));
      await open(tester, locale: const Locale('pt'));
      await tester.tap(find.text('Ler QR code'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField), '  ');

      // act
      await tester.tap(find.text('Confirmar'));
      await tester.pump();

      // assert
      expect(paired, isEmpty);
      expect(find.text('Digite um nome.'), findsOneWidget);
    });
  });
}
