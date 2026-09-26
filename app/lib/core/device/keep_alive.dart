import 'package:flutter_foreground_task/flutter_foreground_task.dart';
import 'package:permission_handler/permission_handler.dart';

/// Keeps the process at foreground priority while the phone is a camera, so Android does
/// not kill it. The work itself stays in the main isolate.
abstract interface class KeepAlive {
  Future<void> start({required String title, required String text});

  Future<void> stop();
}

class ForegroundServiceKeepAlive implements KeepAlive {
  static const _serviceId = 701;

  @override
  Future<void> start({required String title, required String text}) async {
    // Android 14+ refuses a camera or microphone foreground service without its permission.
    await Permission.camera.request();
    // Sound is optional: without the microphone the camera still sends video.
    final microphone = await Permission.microphone.request();
    await FlutterForegroundTask.requestNotificationPermission();
    FlutterForegroundTask.init(
      androidNotificationOptions: AndroidNotificationOptions(
        channelId: 'camera_mode',
        channelName: title,
      ),
      iosNotificationOptions: const IOSNotificationOptions(),
      foregroundTaskOptions: ForegroundTaskOptions(
        eventAction: ForegroundTaskEventAction.nothing(),
        allowWifiLock: true,
      ),
    );
    if (await FlutterForegroundTask.isRunningService) return;
    await FlutterForegroundTask.startService(
      serviceId: _serviceId,
      serviceTypes: [
        ForegroundServiceTypes.camera,
        if (microphone.isGranted) ForegroundServiceTypes.microphone,
      ],
      notificationTitle: title,
      notificationText: text,
      callback: startKeepAliveTask,
    );
  }

  @override
  Future<void> stop() async {
    await FlutterForegroundTask.stopService();
  }
}

@pragma('vm:entry-point')
void startKeepAliveTask() {
  FlutterForegroundTask.setTaskHandler(_IdleTaskHandler());
}

class _IdleTaskHandler extends TaskHandler {
  @override
  Future<void> onStart(DateTime timestamp, TaskStarter starter) async {}

  @override
  void onRepeatEvent(DateTime timestamp) {}

  @override
  Future<void> onDestroy(DateTime timestamp, bool isTimeout) async {}
}
