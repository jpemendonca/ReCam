import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_webrtc/flutter_webrtc.dart';
import 'package:recam/core/media/video_quality.dart';
import 'package:recam/core/media/webrtc_publisher.dart';

void main() {
  group('WhipPublisher.withQuality', () {
    RTCRtpParameters sent() => RTCRtpParameters(
      encodings: [
        RTCRtpEncoding(
          maxBitrate: 700000,
          maxFramerate: 15,
          scaleResolutionDownBy: 1,
        ),
      ],
    );

    test('full_keepsTheResolutionWhateverTheNetwork', () {
      // arrange
      final parameters = sent();

      // act
      final result = WhipPublisher.withQuality(parameters, VideoQuality.full);

      // assert
      expect(
        result.degradationPreference,
        RTCDegradationPreference.MAINTAIN_RESOLUTION,
      );
      expect(result.encodings!.single.scaleResolutionDownBy, 1);
      expect(result.encodings!.single.maxBitrate, 700000);
      expect(result.encodings!.single.maxFramerate, 15);
    });

    test('reduced_lowersBitrateAndFramerateButNotTheResolution', () {
      // arrange
      final parameters = sent();

      // act
      final result = WhipPublisher.withQuality(
        parameters,
        VideoQuality.reduced,
      );

      // assert
      expect(
        result.degradationPreference,
        RTCDegradationPreference.MAINTAIN_RESOLUTION,
      );
      expect(result.encodings!.single.scaleResolutionDownBy, 1);
      expect(result.encodings!.single.maxBitrate, 400000);
      expect(result.encodings!.single.maxFramerate, 10);
    });
  });
}
