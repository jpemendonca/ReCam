import 'dart:async';

import 'package:flutter_test/flutter_test.dart';
import 'package:recam/camera/camera_mode_controller.dart';
import 'package:recam/core/device/battery_reader.dart';
import 'package:recam/core/network/hub_client.dart';
import 'package:recam/core/network/hub_session.dart';

import '../support/fakes.dart';

void main() {
  late FakeHubClient client;
  late FakeBatteryReader battery;
  late FakeScreenController screen;
  late FakeKeepAlive keepAlive;
  late FakePublisher publisher;
  late FakeCapture capture;
  late DateTime now;
  late CameraModeController controller;

  setUp(() {
    client = FakeHubClient();
    battery = FakeBatteryReader();
    screen = FakeScreenController();
    keepAlive = FakeKeepAlive();
    publisher = FakePublisher();
    capture = FakeCapture();
    now = DateTime(2026, 9, 25, 12);
    controller = CameraModeController(
      hub: HubSession(client: client, delay: (_) async {}),
      battery: battery,
      screen: screen,
      keepAlive: keepAlive,
      publisher: publisher,
      capture: capture,
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

  List<Object> telemetry() => [
    for (final call in client.invocations)
      if (call.method == 'ReportTelemetry') call.args.single,
  ];

  List<List<Object>> publishingReports() => [
    for (final call in client.invocations)
      if (call.method == 'ReportPublishing') call.args,
  ];

  group('CameraModeController publishing', () {
    test('onStartPublishing_startsAndReportsTrue', () async {
      // arrange
      await start();

      // act
      client.receive('StartPublishing', []);
      await settle();

      // assert
      expect(publisher.starts, 1);
      expect(controller.publishing, isTrue);
      expect(publishingReports(), [
        [true],
      ]);
    });

    test('onRepeatedStartPublishing_startsOnceAndReportsAgain', () async {
      // arrange
      await start();
      client.receive('StartPublishing', []);
      await settle();

      // act
      client.receive('StartPublishing', []);
      await settle();

      // assert
      expect(publisher.starts, 1);
      expect(publishingReports(), [
        [true],
        [true],
      ]);
    });

    test('onStopPublishing_stopsAndReportsFalse', () async {
      // arrange
      await start();
      client.receive('StartPublishing', []);
      await settle();

      // act
      client.receive('StopPublishing', []);
      await settle();

      // assert
      expect(publisher.stops, 1);
      expect(controller.publishing, isFalse);
      expect(publishingReports().last, [false]);
    });

    test('whenCameraFailsToStart_reportsFalse', () async {
      // arrange
      publisher.startResult = false;
      await start();

      // act
      client.receive('StartPublishing', []);
      await settle();

      // assert
      expect(controller.publishing, isFalse);
      expect(publishingReports(), [
        [false],
      ]);
    });

    test('stop_whilePublishing_releasesTheCamera', () async {
      // arrange
      await start();
      client.receive('StartPublishing', []);
      await settle();

      // act
      await controller.stop();

      // assert
      expect(publisher.stops, 1);
    });
  });

  group('CameraModeController thumbnail', () {
    test(
      'openPreview_withNobodyWatching_opensCameraWithoutPublishing',
      () async {
        // arrange
        await start();

        // act
        await controller.openPreview();

        // assert
        expect(controller.preview, isNotNull);
        expect(capture.opens, 1);
        expect(publisher.starts, 0);
        expect(publishingReports(), isEmpty);
      },
    );

    test('openPreview_whilePublishing_reusesThePublishedTrack', () async {
      // arrange
      await start();
      client.receive('StartPublishing', []);
      await settle();

      // act
      await controller.openPreview();

      // assert
      expect(capture.opens, 1);
      expect(controller.preview, same(publisher.publishedFeed));
    });

    test('closePreview_withNobodyWatching_releasesTheCamera', () async {
      // arrange
      await start();
      await controller.openPreview();

      // act
      await controller.closePreview();

      // assert
      expect(controller.preview, isNull);
      expect(capture.isOpen, isFalse);
      expect(capture.closes, 1);
    });

    test('closePreview_whilePublishing_keepsTheCameraOpen', () async {
      // arrange
      await start();
      client.receive('StartPublishing', []);
      await settle();
      await controller.openPreview();

      // act
      await controller.closePreview();

      // assert
      expect(capture.isOpen, isTrue);
      expect(controller.publishing, isTrue);
    });

    test('startPublishing_withPreviewOpen_publishesTheSameCamera', () async {
      // arrange
      await start();
      await controller.openPreview();
      final previewFeed = controller.preview;

      // act
      client.receive('StartPublishing', []);
      await settle();

      // assert
      expect(capture.opens, 1);
      expect(publisher.publishedFeed, same(previewFeed));
      expect(publishingReports(), [
        [true],
      ]);
    });

    test(
      'stopPublishing_withPreviewOpen_keepsTheCameraAndTurnsTheTorchOff',
      () async {
        // arrange
        await start();
        await controller.openPreview();
        client
          ..receive('StartPublishing', [])
          ..receive('SetTorch', [true]);
        await settle();

        // act
        client.receive('StopPublishing', []);
        await settle();

        // assert
        expect(capture.isOpen, isTrue);
        expect(controller.preview, isNotNull);
        expect(publisher.torchCalls, [true, false]);
      },
    );

    test('stop_withPreviewOpen_releasesTheCamera', () async {
      // arrange
      await start();
      await controller.openPreview();

      // act
      await controller.stop();

      // assert
      expect(capture.isOpen, isFalse);
      expect(controller.preview, isNull);
    });

    test('whenCameraCannotOpen_keepsThePreviewClosed', () async {
      // arrange
      capture.available = false;
      await start();

      // act
      await controller.openPreview();

      // assert
      expect(controller.preview, isNull);
    });
  });

  group('CameraModeController torch', () {
    List<List<Object>> torchReports() => [
      for (final call in client.invocations)
        if (call.method == 'ReportTorch') call.args,
    ];

    Future<void> startPublishing() async {
      await start();
      client.receive('StartPublishing', []);
      await settle();
    }

    test('onSetTorch_whilePublishing_appliesAndReportsOn', () async {
      // arrange
      await startPublishing();

      // act
      client.receive('SetTorch', [true]);
      await settle();

      // assert
      expect(publisher.torchCalls, [true]);
      expect(controller.torchOn, isTrue);
      expect(torchReports(), [
        [true],
      ]);
    });

    test(
      'onSetTorch_whenNotPublishing_reportsOffWithoutTouchingCamera',
      () async {
        // arrange
        await start();

        // act
        client.receive('SetTorch', [true]);
        await settle();

        // assert
        expect(publisher.torchCalls, isEmpty);
        expect(torchReports(), [
          [false],
        ]);
      },
    );

    test('onSetTorch_whenCameraHasNoTorch_reportsCurrentState', () async {
      // arrange
      publisher.torchResult = false;
      await startPublishing();

      // act
      client.receive('SetTorch', [true]);
      await settle();

      // assert
      expect(controller.torchOn, isFalse);
      expect(torchReports(), [
        [false],
      ]);
    });

    test('onStopPublishing_withTorchOn_reportsTorchOff', () async {
      // arrange
      await startPublishing();
      client.receive('SetTorch', [true]);
      await settle();

      // act
      client.receive('StopPublishing', []);
      await settle();

      // assert
      expect(controller.torchOn, isFalse);
      expect(torchReports().last, [false]);
    });
  });

  group('CameraModeController watchers', () {
    test('onWatchersChanged_showsTheCount', () async {
      // arrange
      await start();

      // act
      client.receive('WatchersChanged', [2]);

      // assert
      expect(controller.watchers, 2);
    });

    test('whenDisconnected_showsNobodyWatching', () async {
      // arrange
      final offline = CameraModeController(
        hub: HubSession(client: client, delay: (_) => Completer<void>().future),
        battery: battery,
        screen: screen,
        keepAlive: keepAlive,
        publisher: publisher,
        capture: capture,
      );
      await offline.start(notificationTitle: 't', notificationText: 'x');
      await settle();
      client.receive('WatchersChanged', [2]);

      // act
      client.drop();
      await settle();

      // assert
      expect(offline.watchers, 0);
      await offline.stop();
      offline.dispose();
    });
  });

  group('CameraModeController pairing lost', () {
    test('whenServerRejectsCredential_reportsPairingLost', () async {
      // arrange
      client.connectResults.add(HubConnectOutcome.rejected);

      // act
      await start();

      // assert
      expect(controller.pairingLost, isTrue);
      expect(client.connectCalls, 1);
    });
  });

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
        {'batteryLevel': 64, 'isCharging': false, 'temperatureC': null},
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
      expect(telemetry().last, {
        'batteryLevel': 79,
        'isCharging': true,
        'temperatureC': null,
      });
    });

    test('checkBattery_whenTemperatureChangesADegree_reportsIt', () async {
      // arrange
      battery.reading = const BatteryReading(
        level: 80,
        isCharging: true,
        temperatureC: 36.2,
      );
      await start();
      battery.reading = const BatteryReading(
        level: 80,
        isCharging: true,
        temperatureC: 37.4,
      );

      // act
      await controller.checkBattery();

      // assert
      expect(telemetry(), hasLength(2));
      expect((telemetry().last as Map)['temperatureC'], 37.4);
    });

    test('checkBattery_whenTemperatureMovesATenth_sendsNothing', () async {
      // arrange
      battery.reading = const BatteryReading(
        level: 80,
        isCharging: true,
        temperatureC: 36.2,
      );
      await start();
      battery.reading = const BatteryReading(
        level: 80,
        isCharging: true,
        temperatureC: 36.4,
      );

      // act
      await controller.checkBattery();

      // assert
      expect(telemetry(), hasLength(1));
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
