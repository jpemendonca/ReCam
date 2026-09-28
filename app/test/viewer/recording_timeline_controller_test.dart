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

/// Thirty one-minute segments from 15:00 UTC (12:00 in Brasília).
RecordingPieceInfo _halfHour() => _piece([
  for (var minute = 0; minute < 30; minute++)
    DateTime.utc(2026, 9, 25, 15, minute),
]);

MotionEventInfo _motion(DateTime start, int seconds, {bool? person}) =>
    MotionEventInfo(
      start: start,
      end: start.add(Duration(seconds: seconds)),
      person: person,
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
      peopleBoxes: FakePeopleBoxesStore(),
      utcOffsetOf: _brasilia,
    );
  });

  group('RecordingTimelineController', () {
    test('load_turnsServerDaysIntoThePhoneCalendar', () async {
      // arrange
      api.recordingDaysResult = [DateTime.utc(2026, 9, 25)];
      api.recordingsByDay[DateTime.utc(2026, 9, 25)] = [
        _piece([DateTime.utc(2026, 9, 25, 14)]),
      ];

      // act
      await controller.load();

      // assert
      expect(controller.days, [
        DateTime.utc(2026, 9, 25),
        DateTime.utc(2026, 9, 24),
      ]);
      expect(controller.selectedDay, DateTime.utc(2026, 9, 25));
    });

    test('load_recordedLateAtNight_opensThatDayWithoutAnEmptyToday', () async {
      // arrange
      api.recordingDaysResult = [DateTime.utc(2026, 9, 26)];
      api.recordingsByDay[DateTime.utc(2026, 9, 26)] = [
        _piece([DateTime.utc(2026, 9, 26, 0, 30)]),
      ];

      // act
      await controller.load();

      // assert
      expect(controller.days, [DateTime.utc(2026, 9, 25)]);
      expect(controller.selectedDay, DateTime.utc(2026, 9, 25));
      expect(
        controller.timeline.single.start,
        DateTime.utc(2026, 9, 25, 21, 30),
      );
    });

    test('load_nothingRecorded_hasNoDays', () async {
      // arrange
      api.recordingDaysResult = [DateTime.utc(2026, 9, 25)];

      // act
      await controller.load();

      // assert
      expect(controller.days, isEmpty);
      expect(controller.selectedDay, isNull);
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

    test('nextAndPreviousMotion_playFromFiveSecondsBeforeEachEvent', () async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      api.recordingsByDay[day] = [_halfHour()];
      api.motionByDay[day] = [
        _motion(DateTime.utc(2026, 9, 25, 15, 5), 20),
        _motion(DateTime.utc(2026, 9, 25, 15, 20), 40),
      ];
      await controller.selectDay(day);

      // act
      await controller.playNextMotion();
      final first = controller.playingFrom;
      await controller.playNextMotion();
      final second = controller.playingFrom;
      await controller.playNextMotion();
      final afterLast = controller.playingFrom;
      await controller.playPreviousMotion();

      // assert
      expect(controller.motion.map((mark) => mark.start), [
        DateTime.utc(2026, 9, 25, 12, 5),
        DateTime.utc(2026, 9, 25, 12, 20),
      ]);
      expect(first, DateTime.utc(2026, 9, 25, 12, 4, 55));
      expect(second, DateTime.utc(2026, 9, 25, 12, 19, 55));
      expect(afterLast, second);
      expect(controller.playingFrom, first);
      expect(player.plays.last.from, const Duration(seconds: 55));
    });

    test('onlyMotion_whenASegmentEnds_skipsToTheNextMotion', () async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      api.recordingsByDay[day] = [_halfHour()];
      api.motionByDay[day] = [
        _motion(DateTime.utc(2026, 9, 25, 15, 5, 10), 5),
        _motion(DateTime.utc(2026, 9, 25, 15, 20), 40),
      ];
      await controller.selectDay(day);
      controller.onlyMotion = true;
      await controller.playNextMotion();

      // act
      player.finish();
      await settle();

      // assert
      expect(controller.playingFrom, DateTime.utc(2026, 9, 25, 12, 19, 55));
    });

    test('onlyPeople_skipsMotionWithoutAPerson', () async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      api.recordingsByDay[day] = [_halfHour()];
      api.motionByDay[day] = [
        _motion(DateTime.utc(2026, 9, 25, 15, 5, 10), 5, person: true),
        _motion(DateTime.utc(2026, 9, 25, 15, 10), 20, person: false),
        _motion(DateTime.utc(2026, 9, 25, 15, 20), 40, person: true),
      ];
      await controller.selectDay(day);
      controller.onlyPeople = true;

      // act
      await controller.playNextMotion();
      final first = controller.playingFrom;
      player.finish();
      await settle();

      // assert
      expect(controller.peopleAnalyzed, isTrue);
      expect(controller.people, hasLength(2));
      expect(controller.shownMotion, hasLength(2));
      expect(first, DateTime.utc(2026, 9, 25, 12, 5, 5));
      expect(controller.playingFrom, DateTime.utc(2026, 9, 25, 12, 19, 55));
    });

    test('onlyPeople_notAnalyzed_keepsAllMotion', () async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      api.recordingsByDay[day] = [_halfHour()];
      api.motionByDay[day] = [
        _motion(DateTime.utc(2026, 9, 25, 15, 5), 20),
        _motion(DateTime.utc(2026, 9, 25, 15, 20), 40),
      ];
      await controller.selectDay(day);

      // act
      controller.onlyPeople = true;

      // assert
      expect(controller.peopleAnalyzed, isFalse);
      expect(controller.people, isEmpty);
      expect(controller.shownMotion, hasLength(2));
    });

    test('playMark_playsFromFiveSecondsBefore', () async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      api.recordingsByDay[day] = [_halfHour()];
      api.motionByDay[day] = [
        _motion(DateTime.utc(2026, 9, 25, 15, 12), 8, person: true),
      ];
      await controller.selectDay(day);

      // act
      await controller.playMark(controller.people.single);

      // assert
      expect(controller.playingFrom, DateTime.utc(2026, 9, 25, 12, 11, 55));
    });

    test('play_analyzedSegment_loadsItsPeople', () async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      api.recordingsByDay[day] = [_halfHour()];
      api.peopleBySegment['/api/recordings/cam/${DateTime.utc(2026, 9, 25, 15, 5).toIso8601String()}.mp4'] =
          const SegmentPeople(
            seconds: [
              PeopleSecond(
                at: 3,
                people: [PersonBox(x: 0.1, y: 0.2, width: 0.3, height: 0.4)],
              ),
            ],
          );
      await controller.selectDay(day);

      // act
      await controller.playAt(DateTime.utc(2026, 9, 25, 12, 5));
      await settle();
      final analyzed = controller.playingPeople;
      await controller.playAt(DateTime.utc(2026, 9, 25, 12, 6));
      await settle();

      // assert
      expect(analyzed?.at(3.5), hasLength(1));
      expect(controller.playingPeople, isNull);
    });

    test('setShowPeople_startsFromTheSavedChoiceAndSavesIt', () async {
      // arrange
      final store = FakePeopleBoxesStore()..show = false;
      final other = RecordingTimelineController(
        api: api,
        session: pairedSession(),
        cameraId: 'cam',
        player: player,
        segments: segments,
        peopleBoxes: store,
        utcOffsetOf: _brasilia,
      );
      await other.load();
      final loaded = other.showPeople;

      // act
      await other.setShowPeople(true);

      // assert
      expect(loaded, isFalse);
      expect(other.showPeople, isTrue);
      expect(store.show, isTrue);
    });

    test('onlyMotion_motionPastTheSegmentEnd_playsTheNextSegment', () async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      api.recordingsByDay[day] = [_halfHour()];
      api.motionByDay[day] = [
        _motion(DateTime.utc(2026, 9, 25, 15, 5, 50), 30),
      ];
      await controller.selectDay(day);
      controller.onlyMotion = true;
      await controller.playNextMotion();

      // act
      player.finish();
      await settle();

      // assert
      expect(controller.playing?.start, DateTime.utc(2026, 9, 25, 12, 6));
      expect(player.plays.last.from, Duration.zero);
    });

    test('setSensitivity_savesItAndFindsTheMotionAgain', () async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      api.recordingsByDay[day] = [_halfHour()];
      api.motionByDay[day] = [_motion(DateTime.utc(2026, 9, 25, 15, 5), 20)];
      api.motionAfterChange[day] = [];
      await controller.selectDay(day);

      // act
      await controller.setSensitivity(MotionSensitivity.low);

      // assert
      expect(api.motionSensitivity, MotionSensitivity.low);
      expect(controller.sensitivity, MotionSensitivity.low);
      expect(controller.motion, isEmpty);
      expect(controller.sensitivityFailed, isFalse);
    });

    test('setSensitivity_withoutTheServer_saysItFailed', () async {
      // arrange
      await controller.selectDay(DateTime.utc(2026, 9, 25));
      api.sensitivityFailure = ApiFailureKind.unreachable;

      // act
      await controller.setSensitivity(MotionSensitivity.high);

      // assert
      expect(controller.sensitivityFailed, isTrue);
      expect(controller.sensitivity, MotionSensitivity.medium);
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
