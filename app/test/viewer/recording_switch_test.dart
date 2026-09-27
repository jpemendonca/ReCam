import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/hub_session.dart';
import 'package:recam/l10n/generated/app_localizations.dart';
import 'package:recam/viewer/camera_list_controller.dart';
import 'package:recam/viewer/recording_timeline_controller.dart';
import 'package:recam/viewer/recording_switch.dart';

import '../support/fakes.dart';

void main() {
  late FakeApiClient api;
  late FakeHubClient hubClient;
  late CameraListController list;

  setUp(() {
    api = FakeApiClient();
    hubClient = FakeHubClient()..invokeResult = <String, Object?>{'ok': true};
    list = CameraListController(
      api: api,
      session: pairedSession(),
      hub: HubSession(client: hubClient, delay: (_) async {}),
      viewerFactory: (_) => FakeViewer(),
      timelineFactory: (cameraId) => RecordingTimelineController(
        api: FakeApiClient(),
        session: pairedSession(),
        cameraId: cameraId,
        player: FakeRecordingPlayer(),
        segments: FakeSegmentSource(),
      ),
      adjustments: FakeAdjustmentStore(),
    );
  });

  Future<void> show(WidgetTester tester, CameraInfo camera) async {
    api.cameraResults.add(ApiSuccess([camera]));
    await list.refresh();
    await tester.pumpWidget(
      MaterialApp(
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: Scaffold(
          body: RecordingSwitch(list: list, cameraId: camera.id),
        ),
      ),
    );
  }

  group('RecordingSwitch', () {
    testWidgets('onTap_asksToRecordAlways', (tester) async {
      // arrange
      await show(
        tester,
        const CameraInfo(
          id: 'a',
          name: 'Porch',
          online: true,
          publishing: false,
        ),
      );

      // act
      await tester.tap(find.byType(Switch));
      await tester.pump();

      // assert
      expect(find.text('Recording off'), findsOneWidget);
      expect(hubClient.invocations.single.args, ['a', true]);
    });

    for (final (recording, state, text) in [
      (true, RecordingState.recording, 'Recording'),
      (true, RecordingState.off, 'Starting to record…'),
      (true, RecordingState.starting, 'Starting to record…'),
      (true, RecordingState.offline, 'Not recording: the camera is offline'),
      (
        true,
        RecordingState.noSpace,
        "Not recording: the server's disk is full",
      ),
      (
        true,
        RecordingState.stalled,
        'Not recording: the video does not reach the server',
      ),
      (false, RecordingState.recording, 'Recording off'),
    ]) {
      testWidgets('shows_${state.name}_whenSwitch${recording ? 'On' : 'Off'}', (
        tester,
      ) async {
        // act
        await show(
          tester,
          CameraInfo(
            id: 'a',
            name: 'Porch',
            online: true,
            publishing: true,
            recording: recording,
            recordingState: state,
          ),
        );

        // assert
        expect(find.text(text), findsOneWidget);
        expect(tester.widget<Switch>(find.byType(Switch)).value, recording);
      });
    }

    testWidgets('whenTheFirstFileArrives_startingTurnsIntoRecording', (
      tester,
    ) async {
      // arrange
      const starting = CameraInfo(
        id: 'a',
        name: 'Porch',
        online: true,
        publishing: true,
        recording: true,
        recordingState: RecordingState.starting,
      );
      api.cameraResults.add(ApiSuccess([starting]));
      await list.start();
      await show(tester, starting);
      final before = find.text('Starting to record…').evaluate().length;

      // act
      hubClient.receive('CameraStatusChanged', [
        {
          'id': 'a',
          'name': 'Porch',
          'online': true,
          'publishing': true,
          'recording': true,
          'canRecord': true,
          'recordingState': 'recording',
        },
      ]);
      await tester.pump();

      // assert
      expect(before, 1);
      expect(find.text('Recording'), findsOneWidget);
      await list.stop();
      list.dispose();
    });

    testWidgets('forCameraWithoutH264_isOffWithTheReason', (tester) async {
      // arrange
      await show(
        tester,
        const CameraInfo(
          id: 'a',
          name: 'Porch',
          online: true,
          publishing: false,
          canRecord: false,
        ),
      );

      // act
      await tester.tap(find.byType(Switch));
      await tester.pump();

      // assert
      expect(
        find.text("Can't record: this phone has no H.264"),
        findsOneWidget,
      );
      expect(tester.widget<Switch>(find.byType(Switch)).onChanged, isNull);
      expect(hubClient.invocations, isEmpty);
    });
  });
}
