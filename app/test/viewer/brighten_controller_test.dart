import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/media/image_adjustment.dart';
import 'package:recam/l10n/generated/app_localizations.dart';
import 'package:recam/viewer/brighten_controller.dart';
import 'package:recam/viewer/brighten_panel.dart';

import '../support/fakes.dart';

void main() {
  late FakeAdjustmentStore store;

  setUp(() => store = FakeAdjustmentStore());

  group('BrightenController', () {
    test('change_isRememberedForThatCamera', () async {
      // arrange
      final controller = BrightenController(store: store, cameraId: 'cam');
      await controller.load();

      // act
      controller.setBrightness(1.8);
      controller.setContrast(1.3);
      final again = BrightenController(store: store, cameraId: 'cam');
      await again.load();
      final other = BrightenController(store: store, cameraId: 'other');
      await other.load();

      // assert
      expect(
        again.adjustment,
        const ImageAdjustment(brightness: 1.8, contrast: 1.3),
      );
      expect(other.adjustment.isNormal, isTrue);
    });

    test('reset_goesBackToNormalAndForgets', () async {
      // arrange
      final controller = BrightenController(store: store, cameraId: 'cam');
      controller.setBrightness(2);

      // act
      controller.reset();

      // assert
      expect(controller.adjustment.isNormal, isTrue);
      expect(store.saved, isEmpty);
    });

    test('values_stayInsideTheSliders', () {
      // arrange
      final controller = BrightenController(store: store, cameraId: 'cam');

      // act
      controller.setBrightness(9);
      controller.setContrast(0);

      // assert
      expect(controller.adjustment.brightness, ImageAdjustment.maxBrightness);
      expect(controller.adjustment.contrast, ImageAdjustment.minContrast);
    });
  });

  group('ImageAdjustment', () {
    test('decode_ofEncode_isTheSame', () {
      // arrange
      const adjustment = ImageAdjustment(brightness: 1.5, contrast: 0.8);

      // act
      final decoded = ImageAdjustment.decode(adjustment.encode());

      // assert
      expect(decoded, adjustment);
    });

    test('decode_garbage_isNormal', () {
      expect(ImageAdjustment.decode('oops').isNormal, isTrue);
      expect(ImageAdjustment.decode(null).isNormal, isTrue);
    });
  });

  group('Brighten on screen', () {
    testWidgets('button_opensSliders_andBrighterVideoIsFiltered', (
      tester,
    ) async {
      // arrange
      final controller = BrightenController(store: store, cameraId: 'cam');
      await tester.pumpWidget(
        MaterialApp(
          locale: const Locale('pt'),
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: Scaffold(
            appBar: AppBar(actions: [BrightenButton(controller: controller)]),
            body: Column(
              children: [
                BrightenedVideo(
                  controller: controller,
                  child: const SizedBox(width: 160, height: 90),
                ),
                ListenableBuilder(
                  listenable: controller,
                  builder: (context, _) => controller.open
                      ? BrightenPanel(controller: controller)
                      : const SizedBox.shrink(),
                ),
              ],
            ),
          ),
        ),
      );
      final filteredBefore = find.byType(ColorFiltered).evaluate().length;

      // act
      await tester.tap(find.byTooltip('Clarear'));
      await tester.pump();
      await tester.drag(
        find.byKey(const Key('brighten-brightness')),
        const Offset(200, 0),
      );
      await tester.pump();

      // assert
      expect(filteredBefore, 0);
      expect(find.text('Brilho'), findsOneWidget);
      expect(find.text('Contraste'), findsOneWidget);
      expect(find.byType(ColorFiltered), findsOneWidget);
      expect(controller.adjustment.brightness, greaterThan(1));
      await tester.tap(find.text('Voltar ao normal'));
      await tester.pump();
      expect(find.byType(ColorFiltered), findsNothing);
    });
  });
}
