import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/viewer/recordings_controller.dart';

import '../support/fakes.dart';

const _megabyte = RecordingQuota.bytesPerMegabyte;

void main() {
  late FakeApiClient api;
  late RecordingsController controller;

  setUp(() {
    api = FakeApiClient()
      ..quotaResult = ApiSuccess(
        const RecordingQuota(
          megabytes: 2048,
          usedBytes: 500 * _megabyte,
          freeBytes: 9500 * _megabyte,
        ),
      );
    controller = RecordingsController(api: api, session: pairedSession());
  });

  tearDown(() => controller.dispose());

  RecordingsReady ready() => controller.state as RecordingsReady;

  group('RecordingsController', () {
    test('load_showsTheQuotaAndTheDisk', () async {
      // arrange
      // (server answers 2 GB, 500 MB used, 9.5 GB free)

      // act
      await controller.load();

      // assert
      expect(ready().selectedMegabytes, 2048);
      expect(ready().quota.maxMegabytes, 10000);
    });

    test('select_staysBetweenTheMinimumAndTheDisk', () async {
      // arrange
      await controller.load();

      // act
      controller.select(50);
      final low = ready().selectedMegabytes;
      controller.select(99999);

      // assert
      expect(low, RecordingsController.minimumMegabytes);
      expect(ready().selectedMegabytes, 10000);
    });

    test('hoursFor_countsThreeHundredMegabytesPerCameraHour', () {
      // arrange
      const megabytes = 3000;

      // act
      final hours = RecordingsController.hoursFor(megabytes);

      // assert
      expect(hours, 10);
    });

    test('save_sendsTheSelectedQuota', () async {
      // arrange
      await controller.load();
      controller.select(600);

      // act
      final saved = await controller.save();

      // assert
      expect(saved, isTrue);
      expect(api.quotaChanges, [600]);
      expect(ready().quota.megabytes, 600);
    });

    test('save_whenRefused_keepsTheOldQuota', () async {
      // arrange
      await controller.load();
      controller.select(600);
      api.setQuotaFailure = ApiFailureKind.rejected;

      // act
      final saved = await controller.save();

      // assert
      expect(saved, isFalse);
      expect(ready().quota.megabytes, 2048);
    });

    test('load_whenServerFails_showsFailure', () async {
      // arrange
      api.quotaResult = ApiFailure(ApiFailureKind.unreachable);

      // act
      await controller.load();

      // assert
      expect(controller.state, isA<RecordingsFailed>());
    });
  });
}
