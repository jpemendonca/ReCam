import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/scanner/pairing_code_dialog.dart';
import 'package:recam/l10n/generated/app_localizations.dart';

Widget _host(void Function(String?) onResult) => MaterialApp(
  localizationsDelegates: const [
    AppLocalizations.delegate,
    GlobalMaterialLocalizations.delegate,
    GlobalWidgetsLocalizations.delegate,
  ],
  supportedLocales: AppLocalizations.supportedLocales,
  home: Builder(
    builder: (context) => TextButton(
      onPressed: () async => onResult(await showPairingCodeDialog(context)),
      child: const Text('open'),
    ),
  ),
);

void main() {
  group('showPairingCodeDialog', () {
    testWidgets('withPastedCode_returnsTrimmedText', (tester) async {
      // arrange
      String? result;
      await tester.pumpWidget(_host((value) => result = value));
      await tester.tap(find.text('open'));
      await tester.pumpAndSettle();
      await tester.enterText(
        find.byType(TextField),
        '  recam://pair?v=1&t=x \n',
      );

      // act
      await tester.tap(find.text('Pair'));
      await tester.pumpAndSettle();

      // assert
      expect(result, 'recam://pair?v=1&t=x');
    });

    testWidgets('whenCancelled_returnsNull', (tester) async {
      // arrange
      String? result = 'unchanged';
      await tester.pumpWidget(_host((value) => result = value));
      await tester.tap(find.text('open'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField), 'recam://pair?v=1');

      // act
      await tester.tap(find.text('Cancel'));
      await tester.pumpAndSettle();

      // assert
      expect(result, isNull);
    });

    testWidgets('withBlankText_returnsNull', (tester) async {
      // arrange
      String? result = 'unchanged';
      await tester.pumpWidget(_host((value) => result = value));
      await tester.tap(find.text('open'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField), '   ');

      // act
      await tester.tap(find.text('Pair'));
      await tester.pumpAndSettle();

      // assert
      expect(result, isNull);
    });
  });
}
