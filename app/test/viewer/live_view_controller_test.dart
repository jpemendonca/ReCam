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

  group('LiveViewController sound', () {
    test('toggleMuted_silencesThenPlaysTheSoundAgain', () async {
      // arrange
      final before = controller.muted;

      // act
      await controller.toggleMuted();
      final silenced = viewer.muted;
      await controller.toggleMuted();

      // assert
      expect(before, isFalse);
      expect(silenced, isTrue);
      expect(viewer.muted, isFalse);
      expect(controller.muted, isFalse);
    });
  });

  group('LiveViewController torch', () {
    test('withTorchAlreadyOn_startsShowingItOn', () async {
      // arrange
      final lit = LiveViewController(
        hub: HubSession(client: FakeHubClient(), delay: (_) async {}),
        viewer: FakeViewer(),
        cameraId: 'cam-1',
        torchOn: true,
      );

      // act
      final on = lit.torchOn;

      // assert
      expect(on, isTrue);
      lit.dispose();
    });

    test('setTorch_sendsCommandForThisCamera', () async {
      // arrange
      hubClient.invokeResult = <String, Object?>{'ok': true};
      viewer.startResults.add(true);
      await controller.start();

      // act
      final accepted = await controller.setTorch(true);

      // assert
      expect(accepted, isTrue);
      expect(hubCalls().last, 'SetTorch(cam-1,true)');
    });

    test('setTorch_whenServerRefuses_returnsFalse', () async {
      // arrange
      hubClient.invokeResult = <String, Object?>{
        'ok': false,
        'code': 'media.camera_not_publishing',
      };
      await controller.start();

      // act
      final accepted = await controller.setTorch(true);

      // assert
      expect(accepted, isFalse);
      expect(controller.torchOn, isFalse);
    });

    test('onTorchChanged_forThisCamera_showsReportedState', () async {
      // arrange
      await controller.start();

      // act
      hubClient.receive('TorchChanged', ['CAM-1', true]);

      // assert
      expect(controller.torchOn, isTrue);
    });

    test('onTorchChanged_forOtherCamera_isIgnored', () async {
      // arrange
      await controller.start();

      // act
      hubClient.receive('TorchChanged', ['cam-2', true]);

      // assert
      expect(controller.torchOn, isFalse);
    });

    test('close_stopsListeningToTorch', () async {
      // arrange
      await controller.start();

      // act
      await controller.close();

      // assert
      expect(hubClient.handlers, isNot(contains('TorchChanged')));
    });
  });

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
