import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/viewer/devices_controller.dart';

import '../support/fakes.dart';

const _thisPhone = DeviceInfo(
  id: '0123456789ABCDEF0123456789ABCDEF',
  name: 'Redmi 6A',
  role: DeviceRole.owner,
  online: true,
);
const _camera = DeviceInfo(
  id: 'cam',
  name: 'Samsung A10',
  role: DeviceRole.camera,
  online: true,
);
const _otherMonitor = DeviceInfo(
  id: 'mon',
  name: 'Tablet',
  role: DeviceRole.viewer,
  online: false,
);

void main() {
  late FakeApiClient api;
  late DevicesController controller;

  setUp(() {
    api = FakeApiClient();
    controller = DevicesController(api: api, session: pairedSession());
  });

  tearDown(() => controller.dispose());

  group('DevicesController', () {
    test('load_splitsCamerasAndMonitors', () async {
      // arrange
      api.deviceResults.add(ApiSuccess([_camera, _thisPhone, _otherMonitor]));

      // act
      await controller.load();

      // assert
      final state = controller.state as DevicesLoaded;
      expect(state.cameras, [_camera]);
      expect(state.monitors, [_thisPhone, _otherMonitor]);
    });

    test('remove_asksTheServerAndReloads', () async {
      // arrange
      api.deviceResults.addAll([
        ApiSuccess([_camera, _thisPhone]),
        ApiSuccess([_thisPhone]),
      ]);
      await controller.load();

      // act
      final removed = await controller.remove(_camera);

      // assert
      expect(removed, isTrue);
      expect(api.removedDevices, ['cam']);
      expect((controller.state as DevicesLoaded).cameras, isEmpty);
    });

    test('remove_thisPhone_neverAsks', () async {
      // arrange
      api.deviceResults.add(ApiSuccess([_thisPhone]));
      await controller.load();

      // act
      final removed = await controller.remove(_thisPhone);

      // assert
      expect(removed, isFalse);
      expect(controller.isThisPhone(_thisPhone), isTrue);
      expect(api.removedDevices, isEmpty);
    });

    test('remove_whenRefused_returnsFalse', () async {
      // arrange
      api
        ..deviceResults.add(ApiSuccess([_camera]))
        ..removeFailure = ApiFailureKind.unreachable;
      await controller.load();

      // act
      final removed = await controller.remove(_camera);

      // assert
      expect(removed, isFalse);
    });
  });
}
