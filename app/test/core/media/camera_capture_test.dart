import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/media/camera_capture.dart';

void main() {
  group('PluginCameraCapture.constraints', () {
    test('withAudio_asksForTheMicrophoneAndTheBackCamera', () {
      // arrange
      const withAudio = true;

      // act
      final constraints = PluginCameraCapture.constraints(withAudio: withAudio);

      // assert
      expect(constraints['audio'], isTrue);
      expect(constraints['video'], containsPair('facingMode', 'environment'));
    });

    test('withoutAudio_asksOnlyForTheCamera', () {
      // arrange
      const withAudio = false;

      // act
      final constraints = PluginCameraCapture.constraints(withAudio: withAudio);

      // assert
      expect(constraints['audio'], isFalse);
      expect(constraints['video'], containsPair('height', 720));
    });
  });
}
