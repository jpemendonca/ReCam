import 'package:flutter_test/flutter_test.dart';
import 'package:recam/camera/camera_mode_controller.dart';
import 'package:recam/core/device/battery_reader.dart';
import 'package:recam/core/network/hub_session.dart';

import '../support/fakes.dart';

void main() {
  late FakeHubClient client;
  late FakeBatteryReader battery;
  late FakeScreenController screen;
  late FakeKeepAlive keepAlive;
  late DateTime now;
  late CameraModeController controller;

  setUp(() {
    client = FakeHubClient();
    battery = FakeBatteryReader();
    screen = FakeScreenController();
    keepAlive = FakeKeepAlive();
    now = DateTime(2026, 9, 25, 12);
    controller = CameraModeController(
      hub: HubSession(client: client, delay: (_) async {}),
      battery: battery,
      screen: screen,
      keepAlive: keepAlive,
      now: () => now,
    );
  });

  tearDown(() async {
    await controller.stop();
    controller.dispose();
  });

  Future<void> start() async {
    await controller.start(notificationTitle: 't', notificationText: 'x');
    await settle();
  }

  List<List<Object>> telemetry() => [
    for (final call in client.invocations)
      if (call.method == 'ReportTelemetry') call.args,
  ];

  group('CameraModeController', () {
    test('start_keepsProcessAliveDimsScreenAndReportsOnConnect', () async {
      // arrange
      battery.reading = const BatteryReading(level: 64, isCharging: false);

      // act
      await start();

      // assert
      expect(keepAlive.running, isTrue);
      expect(screen.inCameraMode, isTrue);
      expect(telemetry(), [
        [64, false],
      ]);
    });

    test('checkBattery_whenLevelChanges_reportsAgain', () async {
      // arrange
      await start();
      battery.reading = const BatteryReading(level: 79, isCharging: true);

      // act
      await controller.checkBattery();

      // assert
      expect(telemetry(), hasLength(2));
      expect(telemetry().last, [79, true]);
    });

    test('checkBattery_whenUnchangedAndRecent_sendsNothing', () async {
      // arrange
      await start();
      now = now.add(const Duration(seconds: 30));

      // act
      await controller.checkBattery();

      // assert
      expect(telemetry(), hasLength(1));
    });

    test('checkBattery_whenUnchangedButDue_reportsAgain', () async {
      // arrange
      await start();
      now = now.add(const Duration(seconds: 60));

      // act
      await controller.checkBattery();

      // assert
      expect(telemetry(), hasLength(2));
    });

    test('whenReconnected_reportsAgain', () async {
      // arrange
      await start();

      // act
      client.drop();
      await settle();

      // assert
      expect(telemetry(), hasLength(2));
    });

    test('stop_restoresScreenAndStopsService', () async {
      // arrange
      await start();

      // act
      await controller.stop();

      // assert
      expect(screen.inCameraMode, isFalse);
      expect(keepAlive.running, isFalse);
    });
  });
}
