import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/viewer/recording_timeline_controller.dart';

import '../support/fakes.dart';

/// Brasília time: three hours behind UTC.
Duration _brasilia(DateTime _) => const Duration(hours: -3);

RecordingPieceInfo _piece(List<DateTime> starts) => RecordingPieceInfo(
  start: starts.first,
  end: starts.last.add(const Duration(minutes: 1)),
  segments: [
    for (final start in starts)
      RecordingSegmentInfo(
        start: start,
        end: start.add(const Duration(minutes: 1)),
        url: '/api/recordings/cam/${start.toIso8601String()}.mp4',
      ),
  ],
);

void main() {
  late FakeApiClient api;
  late FakeRecordingPlayer player;
  late FakeSegmentSource segments;
  late RecordingTimelineController controller;

  setUp(() {
    api = FakeApiClient();
    player = FakeRecordingPlayer();
    segments = FakeSegmentSource();
    controller = RecordingTimelineController(
      api: api,
      session: pairedSession(),
      cameraId: 'cam',
      player: player,
      segments: segments,
      utcOffsetOf: _brasilia,
    );
  });

  group('RecordingTimelineController', () {
    test('load_turnsServerDaysIntoThePhoneCalendar', () async {
      // arrange
      api.recordingDaysResult = [DateTime.utc(2026, 9, 25)];

      // act
      await controller.load();

      // assert
      expect(controller.days, [
        DateTime.utc(2026, 9, 25),
        DateTime.utc(2026, 9, 24),
      ]);
      expect(controller.selectedDay, DateTime.utc(2026, 9, 25));
    });

    test('selectDay_fetchesBothUtcDaysAndKeepsOnlyThisLocalDay', () async {
      // arrange
      api.recordingsByDay[DateTime.utc(2026, 9, 25)] = [
        _piece([DateTime.utc(2026, 9, 25, 2)]),
        _piece([
          DateTime.utc(2026, 9, 25, 13),
          DateTime.utc(2026, 9, 25, 13, 1),
        ]),
      ];
      api.recordingsByDay[DateTime.utc(2026, 9, 26)] = [
        _piece([DateTime.utc(2026, 9, 26, 1)]),
      ];

      // act
      await controller.selectDay(DateTime.utc(2026, 9, 25));

      // assert
      expect(api.recordingDayCalls, [
        DateTime.utc(2026, 9, 25),
        DateTime.utc(2026, 9, 26),
      ]);
      expect(controller.timeline.map((segment) => segment.start), [
        DateTime.utc(2026, 9, 25, 10),
        DateTime.utc(2026, 9, 25, 10, 1),
        DateTime.utc(2026, 9, 25, 22),
      ]);
    });

    test('playAt_startsInsideTheTappedSegment', () async {
      // arrange
      api.recordingsByDay[DateTime.utc(2026, 9, 25)] = [
        _piece([
          DateTime.utc(2026, 9, 25, 13),
          DateTime.utc(2026, 9, 25, 13, 1),
        ]),
      ];
      await controller.selectDay(DateTime.utc(2026, 9, 25));

      // act
      await controller.playAt(DateTime.utc(2026, 9, 25, 10, 1, 30));

      // assert
      final play = player.plays.single;
      expect(play.from, const Duration(seconds: 30));
      expect(play.url.path, endsWith('2026-09-25T13:01:00.000Z.mp4'));
    });

    test('playAt_inAGap_startsTheNextRecording', () async {
      // arrange
      api.recordingsByDay[DateTime.utc(2026, 9, 25)] = [
        _piece([DateTime.utc(2026, 9, 25, 13)]),
        _piece([DateTime.utc(2026, 9, 25, 18)]),
      ];
      await controller.selectDay(DateTime.utc(2026, 9, 25));

      // act
      await controller.playAt(DateTime.utc(2026, 9, 25, 12));

      // assert
      expect(player.plays.single.from, Duration.zero);
      expect(controller.playing?.start, DateTime.utc(2026, 9, 25, 15));
    });

    test('whenASegmentEnds_playsTheNextOne', () async {
      // arrange
      api.recordingsByDay[DateTime.utc(2026, 9, 25)] = [
        _piece([
          DateTime.utc(2026, 9, 25, 13),
          DateTime.utc(2026, 9, 25, 13, 1),
        ]),
      ];
      await controller.selectDay(DateTime.utc(2026, 9, 25));
      await controller.playAt(DateTime.utc(2026, 9, 25, 10));

      // act
      player.finish();
      await settle();

      // assert
      expect(player.plays, hasLength(2));
      expect(controller.playing?.start, DateTime.utc(2026, 9, 25, 10, 1));
    });

    test('dispose_releasesThePlayerAndTheRelay', () async {
      // arrange
      await controller.selectDay(DateTime.utc(2026, 9, 25));

      // act
      controller.dispose();
      await settle();

      // assert
      expect(player.disposed, isTrue);
      expect(segments.closed, isTrue);
    });
  });
}
