import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/app.dart';
import 'package:recam/core/network/pinned_http_overrides.dart';
import 'package:recam/core/pairing/pairing_service.dart';
import 'package:recam/core/storage/credential_store.dart';
import 'package:recam/viewer/viewer_pairing_controller.dart';

import 'support/fakes.dart';

void main() {
  late FakeApiClient api;
  late MemoryCredentialStore store;
  late ViewerPairingController viewerPairing;

  setUp(() {
    api = FakeApiClient();
    store = MemoryCredentialStore();
    viewerPairing = ViewerPairingController(
      pairing: PairingService(
        api: api,
        store: store,
        pins: PinnedHttpOverrides(),
      ),
    );
  });

  tearDown(() => viewerPairing.dispose());

  group('RecamApp', () {
    testWidgets('whenTappingWatchTab_showsScanButtonWhileNotPaired', (
      tester,
    ) async {
      // arrange
      await viewerPairing.load();
      await tester.pumpWidget(RecamApp(viewerPairing: viewerPairing, api: api));
      await tester.pumpAndSettle();

      // act
      await tester.tap(find.byIcon(Icons.live_tv_outlined));
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Scan QR code'), findsOneWidget);
    });

    testWidgets('whenTappingWatchTab_showsServerAndRoleWhenPaired', (
      tester,
    ) async {
      // arrange
      await store.write(PairingSlot.viewer, pairedSession());
      await viewerPairing.load();
      await tester.pumpWidget(RecamApp(viewerPairing: viewerPairing, api: api));
      await tester.pumpAndSettle();

      // act
      await tester.tap(find.byIcon(Icons.live_tv_outlined));
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Paired with Recam'), findsOneWidget);
      expect(find.text('Role: owner'), findsOneWidget);
      expect(find.text('Add camera'), findsOneWidget);
    });

    testWidgets('onStart_showsCameraTab', (tester) async {
      // arrange
      await viewerPairing.load();
      await tester.pumpWidget(RecamApp(viewerPairing: viewerPairing, api: api));

      // act
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Use this phone as a camera.'), findsOneWidget);
    });
  });
}
