import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/media/image_adjustment.dart';
import '../core/storage/adjustment_store.dart';

/// "Brighten" for one camera on this phone: opens the sliders, remembers the setting per
/// camera and goes back to normal.
class BrightenController extends ChangeNotifier {
  BrightenController({required this._store, required this.cameraId});

  final AdjustmentStore _store;
  final String cameraId;
  ImageAdjustment _adjustment = ImageAdjustment.normal;
  bool _open = false;

  ImageAdjustment get adjustment => _adjustment;

  /// Whether the sliders show.
  bool get open => _open;

  Future<void> load() async {
    _adjustment = await _store.read(cameraId);
    notifyListeners();
  }

  void toggle() {
    _open = !_open;
    notifyListeners();
  }

  void setBrightness(double value) =>
      _change(_adjustment.copyWith(brightness: value));

  void setContrast(double value) =>
      _change(_adjustment.copyWith(contrast: value));

  void reset() => _change(ImageAdjustment.normal);

  void _change(ImageAdjustment adjustment) {
    _adjustment = adjustment;
    notifyListeners();
    unawaited(_store.write(cameraId, adjustment));
  }
}
