/// What the camera sends. Full is the SPECS.md 2.3 ceiling; reduced keeps a hot phone from
/// overheating (SPECS.md 11). Both keep the capture's resolution: a recording whose picture
/// changes size midway does not play in the browser (bullet 11.9).
enum VideoQuality {
  full(maxBitrate: 700000, maxFramerate: 15),
  reduced(maxBitrate: 400000, maxFramerate: 10);

  const VideoQuality({required this.maxBitrate, required this.maxFramerate});

  final int maxBitrate;
  final int maxFramerate;

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
