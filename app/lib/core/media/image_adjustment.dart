import 'dart:ui';

/// Brightness and contrast applied to the video on this screen only ("Brighten"). The camera,
/// what it sends and the recordings stay as they are.
class ImageAdjustment {
  const ImageAdjustment({this.brightness = 1, this.contrast = 1});

  static const normal = ImageAdjustment();

  static const minBrightness = 1.0;
  static const maxBrightness = 3.0;
  static const minContrast = 0.5;
  static const maxContrast = 2.0;

  /// 1 is the picture as it arrives; 2 doubles every channel.
  final double brightness;

  /// 1 is the picture as it arrives; above it, darks get darker and lights lighter.
  final double contrast;

  bool get isNormal => brightness == 1 && contrast == 1;

  /// Contrast around the middle gray, then brightness, as a color matrix.
  ColorFilter get filter {
    final scale = contrast * brightness;
    final offset = 127.5 * (1 - contrast) * brightness;
    return ColorFilter.matrix([
      scale, 0, 0, 0, offset, //
      0, scale, 0, 0, offset, //
      0, 0, scale, 0, offset, //
      0, 0, 0, 1, 0, //
    ]);
  }

  ImageAdjustment copyWith({double? brightness, double? contrast}) =>
      ImageAdjustment(
        brightness: (brightness ?? this.brightness).clamp(
          minBrightness,
          maxBrightness,
        ),
        contrast: (contrast ?? this.contrast).clamp(minContrast, maxContrast),
      );

  String encode() => '$brightness;$contrast';

  static ImageAdjustment decode(String? value) {
    final parts = value?.split(';') ?? const [];
    final brightness = double.tryParse(parts.firstOrNull ?? '');
    final contrast = double.tryParse(parts.elementAtOrNull(1) ?? '');
    if (brightness == null || contrast == null) return normal;
    return normal.copyWith(brightness: brightness, contrast: contrast);
  }

  @override
  bool operator ==(Object other) =>
      other is ImageAdjustment &&
      other.brightness == brightness &&
      other.contrast == contrast;

  @override
  int get hashCode => Object.hash(brightness, contrast);
}
