import 'package:wakelock_plus/wakelock_plus.dart';

/// The screen while the phone works as a camera: it stays on, so Android does not sleep.
abstract interface class ScreenController {
  Future<void> enterCameraMode();

  Future<void> exitCameraMode();
}

class PluginScreenController implements ScreenController {
  @override
  Future<void> enterCameraMode() => WakelockPlus.enable();

  @override
  Future<void> exitCameraMode() => WakelockPlus.disable();
}
