import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/hub_session.dart';
import 'package:recam/viewer/live_view_controller.dart';

import '../support/fakes.dart';

void main() {
  late FakeHubClient hubClient;
  late FakeViewer viewer;
  late List<Duration> delays;
  late LiveViewController controller;

  setUp(() {
    hubClient = FakeHubClient();
    viewer = FakeViewer();
    delays = [];
    controller = LiveViewController(
      hub: HubSession(client: hubClient, delay: (_) async {}),
      viewer: viewer,
      cameraId: 'cam-1',
      delay: (duration) async => delays.add(duration),
      maxAttempts: 5,
    );
  });

  List<String> hubCalls() => [
    for (final call in hubClient.invocations)
      '${call.method}(${call.args.join(',')})',
  ];

  group('LiveViewController', () {
    test('start_opensLeaseAndRetriesUntilCameraPublishes', () async {
      // arrange
      viewer.startResults.addAll([false, false, true]);

      // act
      await controller.start();

      // assert
      expect(hubCalls(), ['WatchCamera(cam-1)']);
      expect(viewer.starts, 3);
      expect(delays, hasLength(2));
      expect(controller.state, isA<LivePlaying>());
    });

    test('start_whenCameraNeverPublishes_failsAfterMaxAttempts', () async {
      // arrange
      // (viewer never starts)

      // act
      await controller.start();

      // assert
      expect(viewer.starts, 5);
      expect(controller.state, isA<LiveFailed>());
    });

    test('close_releasesLeaseAndViewer', () async {
      // arrange
      viewer.startResults.add(true);
      await controller.start();

      // act
      await controller.close();

      // assert
      expect(hubCalls().last, 'UnwatchCamera(cam-1)');
      expect(viewer.disposed, isTrue);
    });

    test('whenStreamDrops_reconnects', () async {
      // arrange
      viewer.startResults.addAll([true, true]);
      await controller.start();

      // act
      viewer.ended!();
      await settle();

      // assert
      expect(viewer.stops, 1);
      expect(viewer.starts, 2);
      expect(controller.state, isA<LivePlaying>());
    });
  });
}
