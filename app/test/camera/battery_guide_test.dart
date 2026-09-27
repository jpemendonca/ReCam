import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/camera/battery_guide.dart';
import 'package:recam/camera/battery_guide_screen.dart';
import 'package:recam/l10n/generated/app_localizations.dart';

import '../support/fakes.dart';

void main() {
  late FakeBatteryOptimization optimization;
  late BatteryGuideController controller;

  setUp(() {
    optimization = FakeBatteryOptimization();
    controller = BatteryGuideController(optimization: optimization);
  });

  group('BatteryGuideController', () {
    for (final (maker, guide) in [
      ('samsung', BatteryGuide.samsung),
      ('Xiaomi', BatteryGuide.xiaomi),
      ('Redmi', BatteryGuide.xiaomi),
      ('motorola', BatteryGuide.generic),
    ]) {
      test('on_${maker}_givesThe_${guide.name}_tips', () async {
        // arrange
        optimization.maker = maker;

        // act
        final status = await controller.status();

        // assert
        expect(status.guide, guide);
      });
    }

    test('withEverythingInPlace_isAllGood', () async {
      // act
      final status = await controller.status();

      // assert
      expect(status.allGood, isTrue);
    });

    test('readsEachCheckOnItsOwn', () async {
      // arrange
      optimization
        ..ignored = false
        ..backgroundRestricted = true
        ..notifications = false;

      // act
      final status = await controller.status();

      // assert
      expect(status.missing, {
        BatteryCheck.optimization,
        BatteryCheck.background,
        BatteryCheck.notifications,
      });
    });

    test('fix_opensTheRightAndroidScreen', () async {
      // act
      await controller.fix(BatteryCheck.optimization);
      await controller.fix(BatteryCheck.background);
      await controller.fix(BatteryCheck.notifications);

      // assert
      expect(optimization.requests, 1);
      expect(optimization.settingsOpened, 1);
      expect(optimization.notificationRequests, 1);
    });
  });

  group('BatteryGuideScreen', () {
    Future<void> show(WidgetTester tester) async {
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: BatteryGuideScreen(controller: controller),
        ),
      );
      await tester.pumpAndSettle();
    }

    Finder fixButton(BatteryCheck check) => find.descendant(
      of: find.byKey(Key('battery-check-${check.name}')),
      matching: find.text('Fix'),
    );

    testWidgets('showsEachCheckFixedOrWithAFixButton', (tester) async {
      // arrange
      optimization.ignored = false;

      // act
      await show(tester);

      // assert
      expect(fixButton(BatteryCheck.optimization), findsOneWidget);
      expect(fixButton(BatteryCheck.background), findsNothing);
      expect(fixButton(BatteryCheck.notifications), findsNothing);
      expect(find.byIcon(Icons.check_circle), findsNWidgets(2));
    });

    testWidgets('comingBackFromAndroidSettings_checksAgain', (tester) async {
      // arrange
      optimization.backgroundRestricted = true;
      await show(tester);
      await tester.tap(fixButton(BatteryCheck.background));
      await tester.pumpAndSettle();

      // act
      optimization.backgroundRestricted = false;
      tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.paused);
      tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.resumed);
      await tester.pumpAndSettle();

      // assert
      expect(optimization.settingsOpened, 1);
      expect(fixButton(BatteryCheck.background), findsNothing);
      expect(find.byIcon(Icons.check_circle), findsNWidgets(3));
    });

    testWidgets('onXiaomi_tipsAreTextOnly', (tester) async {
      // arrange
      optimization.maker = 'Xiaomi';

      // act
      await show(tester);

      // assert
      expect(find.textContaining('Autostart'), findsOneWidget);
      expect(find.textContaining('lock ReCam'), findsOneWidget);
      expect(find.byType(Checkbox), findsNothing);
      expect(find.text('Continue to camera mode'), findsOneWidget);
    });
  });
}
