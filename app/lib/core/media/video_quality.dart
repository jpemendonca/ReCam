/// What the camera sends. Full is the SPECS.md 2.3 ceiling; reduced keeps a hot phone from
/// overheating (SPECS.md 11).
enum VideoQuality {
  full(maxBitrate: 700000, maxFramerate: 15, scaleDownBy: 1),

  /// 854x480 from the 1280x720 capture.
  reduced(maxBitrate: 400000, maxFramerate: 10, scaleDownBy: 1.5);

  const VideoQuality({
    required this.maxBitrate,
    required this.maxFramerate,
    required this.scaleDownBy,
  });

  final int maxBitrate;
  final int maxFramerate;
  final double scaleDownBy;

  static const reduceAtC = 42.0;
  static const restoreBelowC = 38.0;

  /// The quality to use after a temperature reading. Between the two thresholds the current
  /// quality stays, so a phone near the limit does not switch back and forth. An unknown
  /// temperature changes nothing.
  static VideoQuality forTemperature(
    VideoQuality current,
    double? temperatureC,
  ) {
    if (temperatureC == null) return current;
    if (temperatureC >= reduceAtC) return reduced;
    if (temperatureC < restoreBelowC) return full;
    return current;
  }
}
