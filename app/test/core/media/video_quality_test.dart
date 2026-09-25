import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/media/video_quality.dart';

void main() {
  group('VideoQuality.forTemperature', () {
    test('atFortyTwo_reduces', () {
      // arrange
      const current = VideoQuality.full;

      // act
      final next = VideoQuality.forTemperature(current, 42);

      // assert
      expect(next, VideoQuality.reduced);
    });

    test('justBelowFortyTwo_staysFull', () {
      // arrange
      const current = VideoQuality.full;

      // act
      final next = VideoQuality.forTemperature(current, 41.9);

      // assert
      expect(next, VideoQuality.full);
    });

    test('reducedBetweenThirtyEightAndFortyTwo_staysReduced', () {
      // arrange
      const current = VideoQuality.reduced;

      // act
      final next = VideoQuality.forTemperature(current, 39);

      // assert
      expect(next, VideoQuality.reduced);
    });

    test('reducedAtThirtyEight_staysReduced', () {
      // arrange
      const current = VideoQuality.reduced;

      // act
      final next = VideoQuality.forTemperature(current, 38);

      // assert
      expect(next, VideoQuality.reduced);
    });

    test('reducedBelowThirtyEight_restoresFull', () {
      // arrange
      const current = VideoQuality.reduced;

      // act
      final next = VideoQuality.forTemperature(current, 37.9);

      // assert
      expect(next, VideoQuality.full);
    });

    test('withUnknownTemperature_keepsCurrent', () {
      // arrange
      const current = VideoQuality.reduced;

      // act
      final next = VideoQuality.forTemperature(current, null);

      // assert
      expect(next, VideoQuality.reduced);
    });

    test('reduced_isAbout480pAt10FpsAnd400Kbps', () {
      // arrange
      const quality = VideoQuality.reduced;

      // act
      final width = (1280 / quality.scaleDownBy).round();
      final height = (720 / quality.scaleDownBy).round();

      // assert
      expect(width, 853);
      expect(height, 480);
      expect(quality.maxFramerate, 10);
      expect(quality.maxBitrate, 400000);
    });
  });
}
