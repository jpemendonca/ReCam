import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/hub_client.dart';
import 'package:recam/core/network/hub_session.dart';
import 'package:recam/viewer/camera_list_controller.dart';

import '../support/fakes.dart';

CameraInfo _camera(
  String id,
  String name, {
  bool online = false,
  int? battery,
}) => CameraInfo(
  id: id,
  name: name,
  online: online,
  publishing: false,
  batteryLevel: battery,
);

Map<String, Object?> _statusJson(
  String id,
  String name, {
  bool online = true,
  int? battery,
  bool? charging,
}) => {
  'id': id,
  'name': name,
  'online': online,
  'publishing': false,
  'batteryLevel': battery,
  'isCharging': charging,
  'telemetryAt': null,
};

void main() {
  late FakeApiClient api;
  late FakeHubClient hubClient;
  late CameraListController controller;

  setUp(() {
    api = FakeApiClient();
    hubClient = FakeHubClient();
    controller = CameraListController(
      api: api,
      session: pairedSession(),
      hub: HubSession(client: hubClient, delay: (_) async {}),
      viewerFactory: (_) => FakeViewer(),
    );
  });

  tearDown(() async {
    await controller.stop();
    controller.dispose();
  });

  List<CameraInfo> cameras() => (controller.state as CameraListLoaded).cameras;

  group('CameraListController', () {
    test('onStatusChanged_updatesTheMatchingCamera', () async {
      // arrange
      api.cameraResults.addAll([
        ApiSuccess([_camera('a', 'Kitchen'), _camera('b', 'Porch')]),
        ApiSuccess([_camera('a', 'Kitchen'), _camera('b', 'Porch')]),
      ]);
      await controller.start();
      await settle();

      // act
      hubClient.receive('CameraStatusChanged', [
        _statusJson('b', 'Porch', battery: 42, charging: true),
      ]);

      // assert
      final porch = cameras().singleWhere((camera) => camera.id == 'b');
      expect(porch.online, isTrue);
      expect(porch.batteryLevel, 42);
      expect(porch.isCharging, isTrue);
      expect(cameras(), hasLength(2));
    });

    test('onStatusChanged_forNewCamera_addsItInNameOrder', () async {
      // arrange
      api.cameraResults.addAll([
        ApiSuccess([_camera('b', 'Porch')]),
        ApiSuccess([_camera('b', 'Porch')]),
      ]);
      await controller.start();
      await settle();

      // act
      hubClient.receive('CameraStatusChanged', [_statusJson('a', 'Garage')]);

      // assert
      expect(cameras().map((camera) => camera.name), ['Garage', 'Porch']);
    });

    test('whenHubReconnects_reloadsTheList', () async {
      // arrange
      await controller.start();
      await settle();
      final callsBefore = api.cameraCalls;
      api.cameraResults.add(
        ApiSuccess([_camera('a', 'Kitchen', online: true)]),
      );

      // act
      hubClient.drop();
      await settle();

      // assert
      expect(api.cameraCalls, callsBefore + 1);
      expect(cameras().single.name, 'Kitchen');
    });

    test('whenFirstLoadFails_showsFailure', () async {
      // arrange
      api.cameraResults.addAll([
        ApiFailure(ApiFailureKind.unreachable),
        ApiFailure(ApiFailureKind.unreachable),
      ]);

      // act
      await controller.start();
      await settle();

      // assert
      expect(controller.state, isA<CameraListFailed>());
    });
  });

  group('CameraListController recording', () {
    test('setRecording_asksTheServerForThisCamera', () async {
      // arrange
      hubClient.invokeResult = <String, Object?>{'ok': true};

      // act
      final accepted = await controller.setRecording('a', true);

      // assert
      expect(accepted, isTrue);
      final call = hubClient.invocations.single;
      expect(call.method, 'SetRecording');
      expect(call.args, ['a', true]);
    });

    test('setRecording_whenRefused_returnsFalse', () async {
      // arrange
      hubClient.invokeResult = <String, Object?>{
        'ok': false,
        'code': 'media.recording_needs_h264',
      };

      // act
      final accepted = await controller.setRecording('a', true);

      // assert
      expect(accepted, isFalse);
    });

    test('onStatusChanged_withRecording_showsIt', () async {
      // arrange
      api.cameraResults.addAll([
        ApiSuccess([_camera('a', 'Kitchen')]),
        ApiSuccess([_camera('a', 'Kitchen')]),
      ]);
      await controller.start();
      await settle();

      // act
      hubClient.receive('CameraStatusChanged', [
        {..._statusJson('a', 'Kitchen'), 'recording': true, 'canRecord': true},
      ]);

      // assert
      expect(controller.camera('a')?.recording, isTrue);
    });
  });

  group('CameraListController temperature', () {
    test('onStatusChanged_withTemperature_showsIt', () async {
      // arrange
      api.cameraResults.addAll([
        ApiSuccess([_camera('a', 'Kitchen')]),
        ApiSuccess([_camera('a', 'Kitchen')]),
      ]);
      await controller.start();
      await settle();

      // act
      hubClient.receive('CameraStatusChanged', [
        {..._statusJson('a', 'Kitchen', battery: 50), 'temperatureC': 41.7},
      ]);

      // assert
      expect(cameras().single.temperatureC, 41.7);
    });
  });

  group('CameraListController.refresh', () {
    test('afterLoad_reloadsFromTheApi', () async {
      // arrange
      api.cameraResults.addAll([
        ApiSuccess([_camera('a', 'Kitchen')]),
        ApiSuccess([
          _camera('a', 'Kitchen', online: true),
          _camera('b', 'Porch'),
        ]),
      ]);
      await controller.refresh();

      // act
      await controller.refresh();

      // assert
      expect(api.cameraCalls, 2);
      expect(cameras().map((camera) => camera.name), ['Kitchen', 'Porch']);
      expect(cameras().first.online, isTrue);
      expect(controller.refreshing, isFalse);
    });

    test('whenReloadFails_keepsTheLastList', () async {
      // arrange
      api.cameraResults.addAll([
        ApiSuccess([_camera('a', 'Kitchen')]),
        ApiFailure(ApiFailureKind.unreachable),
      ]);
      await controller.refresh();

      // act
      await controller.refresh();

      // assert
      expect(cameras().single.name, 'Kitchen');
    });

    test('whileLoading_reportsRefreshing', () async {
      // arrange
      final seen = <bool>[];
      controller.addListener(() => seen.add(controller.refreshing));

      // act
      await controller.refresh();

      // assert
      expect(seen.first, isTrue);
      expect(seen.last, isFalse);
    });
  });

  group('CameraListController pairing lost', () {
    test('whenApiRefusesCredential_reportsPairingLost', () async {
      // arrange
      api.cameraResults.add(ApiFailure(ApiFailureKind.unauthorized));

      // act
      await controller.refresh();

      // assert
      expect(controller.pairingLost, isTrue);
    });

    test('whenHubRejectsConnection_reportsPairingLost', () async {
      // arrange
      hubClient.connectResults.add(HubConnectOutcome.rejected);

      // act
      await controller.start();
      await settle();

      // assert
      expect(controller.pairingLost, isTrue);
    });

    test('whenServerIsOffline_keepsThePairing', () async {
      // arrange
      api.cameraResults.add(ApiFailure(ApiFailureKind.unreachable));

      // act
      await controller.refresh();

      // assert
      expect(controller.pairingLost, isFalse);
    });
  });
}
