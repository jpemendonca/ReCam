import 'package:flutter_test/flutter_test.dart';
import 'package:recam/camera/battery_guide.dart';

import '../support/fakes.dart';

void main() {
  late FakeBatteryOptimization optimization;
  late BatteryGuideController controller;

  setUp(() {
    optimization = FakeBatteryOptimization()..ignored = false;
    controller = BatteryGuideController(optimization: optimization);
  });

  group('BatteryGuideController.check', () {
    test('onSamsung_showsTheSamsungSteps', () async {
      // arrange
      optimization.maker = 'samsung';

      // act
      final guide = await controller.check();

      // assert
      expect(guide, BatteryGuide.samsung);
    });

    test('onXiaomi_showsTheXiaomiSteps', () async {
      // arrange
      optimization.maker = 'Xiaomi';

      // act
      final guide = await controller.check();

      // assert
      expect(guide, BatteryGuide.xiaomi);
    });

    test('onRedmiReportedAsMaker_showsTheXiaomiSteps', () async {
      // arrange
      optimization.maker = 'Redmi';

      // act
      final guide = await controller.check();

      // assert
      expect(guide, BatteryGuide.xiaomi);
    });

    test('onOtherMaker_showsTheGenericSteps', () async {
      // arrange
      optimization.maker = 'motorola';

      // act
      final guide = await controller.check();

      // assert
      expect(guide, BatteryGuide.generic);
    });

    test('whenAlreadyFreed_showsNothing', () async {
      // arrange
      optimization.ignored = true;

      // act
      final guide = await controller.check();

      // assert
      expect(guide, isNull);
    });
  });
}
