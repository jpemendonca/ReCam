import 'package:flutter/services.dart';
import 'package:screen_brightness/screen_brightness.dart';
import 'package:wakelock_plus/wakelock_plus.dart';

/// The screen while the phone works as a camera: always on, as dark as possible.
abstract interface class ScreenController {
  Future<void> enterCameraMode();

  Future<void> exitCameraMode();
}

class PluginScreenController implements ScreenController {
  @override
  Future<void> enterCameraMode() async {
    await WakelockPlus.enable();
    await ScreenBrightness.instance.setApplicationScreenBrightness(0);
    await SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
  }

  @override
  Future<void> exitCameraMode() async {
    await SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge);
    await ScreenBrightness.instance.resetApplicationScreenBrightness();
    await WakelockPlus.disable();
  }
}
