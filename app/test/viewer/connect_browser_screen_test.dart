import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/l10n/generated/app_localizations.dart';
import 'package:recam/viewer/connect_browser_screen.dart';

import '../support/fakes.dart';

void main() {
  group('ConnectBrowserScreen', () {
    testWidgets('scan_browserCode_saysConnected', (tester) async {
      // arrange
      final api = FakeApiClient();
      await tester.pumpWidget(
        MaterialApp(
          locale: const Locale('pt'),
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: ConnectBrowserScreen(
            api: api,
            session: pairedSession(),
            readCode: (_) async => 'recam://connect-browser?v=1&l=0123456789abcdef0123456789abcdef&s=secret',
          ),
        ),
      );

      // act
      await tester.tap(find.text('Ler QR code'));
      await tester.pumpAndSettle();

      // assert
      expect(
        find.text('Navegador conectado. Ele já mostra as câmeras.'),
        findsOneWidget,
      );
      expect(api.approvedLinks, hasLength(1));
    });
  });
}
