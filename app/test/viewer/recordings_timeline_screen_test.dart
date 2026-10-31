import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/l10n/generated/app_localizations.dart';
import 'package:recam/viewer/brighten_controller.dart';
import 'package:recam/viewer/recording_timeline_controller.dart';
import 'package:recam/viewer/recordings_timeline_screen.dart';
import 'package:recam/viewer/video_clock.dart';

import '../support/fakes.dart';

void main() {
  group('RecordingsTimelineScreen', () {
    testWidgets('tappingTheHours_playsFromThere', (tester) async {
      // arrange
      final api = FakeApiClient()
        ..recordingDaysResult = [DateTime.utc(2026, 9, 25)]
        ..recordingsByDay[DateTime.utc(2026, 9, 25)] = [
          RecordingPieceInfo(
            start: DateTime.utc(2026, 9, 25),
            end: DateTime.utc(2026, 9, 26),
            segments: [
              RecordingSegmentInfo(
                start: DateTime.utc(2026, 9, 25),
                end: DateTime.utc(2026, 9, 26),
                url: '/api/recordings/cam/all-day.mp4',
              ),
            ],
          ),
        ];
      final player = FakeRecordingPlayer();
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: RecordingsTimelineScreen(
            brighten: () => BrightenController(
              store: FakeAdjustmentStore(),
              cameraId: 'cam',
            ),
            cameraName: 'Porch',
            create: () => RecordingTimelineController(
              api: api,
              session: pairedSession(),
              cameraId: 'cam',
              player: player,
              segments: FakeSegmentSource(),
              peopleBoxes: FakePeopleBoxesStore(),
              utcOffsetOf: (_) => Duration.zero,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      final waiting = find
          .text(
            'Drag the bar to go back or ahead, or tap a moment to watch from there.',
          )
          .evaluate()
          .length;

      // act
      await tester.ensureVisible(find.byKey(const Key('timeline-hours')));
      await tester.pumpAndSettle();
      // The window opens centered on the recording's end, midnight; a quarter in is 23:52:30.
      final hours = tester.getRect(find.byKey(const Key('timeline-hours')));
      await tester.tapAt(hours.centerLeft + Offset(hours.width / 4, 0));
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Recordings · Porch'), findsOneWidget);
      expect(find.byType(ChoiceChip), findsNWidgets(1));
      expect(waiting, 1);
      expect(
        player.plays.single.url.path,
        endsWith('/api/recordings/cam/all-day.mp4'),
      );
      expect(player.plays.single.from, greaterThan(const Duration(hours: 11)));
      expect(find.byKey(const Key('recording-video')), findsOneWidget);
    });

    testWidgets('motion_isMarkedOnTheBarAndPlayedByNextMotion', (tester) async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      final api = FakeApiClient()
        ..recordingDaysResult = [day]
        ..recordingsByDay[day] = [
          RecordingPieceInfo(
            start: DateTime.utc(2026, 9, 25, 12),
            end: DateTime.utc(2026, 9, 25, 13),
            segments: [
              RecordingSegmentInfo(
                start: DateTime.utc(2026, 9, 25, 12),
                end: DateTime.utc(2026, 9, 25, 13),
                url: '/api/recordings/cam/noon.mp4',
              ),
            ],
          ),
        ]
        ..motionByDay[day] = [
          MotionEventInfo(
            start: DateTime.utc(2026, 9, 25, 12, 30),
            end: DateTime.utc(2026, 9, 25, 12, 31),
          ),
        ]
        ..motionAfterChange[day] = [];
      final player = FakeRecordingPlayer();
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: RecordingsTimelineScreen(
            brighten: () => BrightenController(
              store: FakeAdjustmentStore(),
              cameraId: 'cam',
            ),
            cameraName: 'Porch',
            create: () => RecordingTimelineController(
              api: api,
              session: pairedSession(),
              cameraId: 'cam',
              player: player,
              segments: FakeSegmentSource(),
              peopleBoxes: FakePeopleBoxesStore(),
              utcOffsetOf: (_) => Duration.zero,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('timeline-hours')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('1 h'));
      await tester.pumpAndSettle();
      final bar = tester.getRect(find.byKey(const Key('timeline-hours')));
      final mark = tester.getRect(find.byKey(const Key('event-motion')));
      final icons = find
          .byKey(const Key('event-person-icon'))
          .evaluate()
          .length;

      // act
      await tester.scrollUntilVisible(
        find.text('Low'),
        200,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.byKey(const Key('next-motion')));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.text('Low'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Low'));
      await tester.pumpAndSettle();

      // assert
      // The hour around the latest recording's end, 12:30 to 13:30, starts with the motion,
      // a minute: narrower than the icon, so the pill keeps its minimum width around it.
      expect(mark.center.dx - bar.left, closeTo(bar.width / 120, 1));
      expect(mark.width, 22);
      expect(mark.bottom, lessThanOrEqualTo(bar.top));
      expect(icons, 0);
      expect(
        player.plays.single.from,
        const Duration(minutes: 29, seconds: 55),
      );
      expect(api.motionSensitivity, MotionSensitivity.low);
      expect(find.byKey(const Key('event-motion')), findsNothing);
      expect(find.text('No motion on this day.'), findsOneWidget);
    });

    testWidgets('people_areYellowEventsWithTheIconAndPlayWhenTapped', (
      tester,
    ) async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      final api = _noonApi(day)
        ..motionByDay[day] = [
          MotionEventInfo(
            start: DateTime.utc(2026, 9, 25, 12, 40),
            end: DateTime.utc(2026, 9, 25, 12, 41),
            person: true,
          ),
          MotionEventInfo(
            start: DateTime.utc(2026, 9, 25, 12, 50),
            end: DateTime.utc(2026, 9, 25, 12, 51),
            person: false,
          ),
        ];
      final player = FakeRecordingPlayer();
      await tester.pumpWidget(_screen(api, player));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('timeline-hours')));
      await tester.pumpAndSettle();
      // The hour around 13:00 holds both events.
      await tester.tap(find.text('1 h'));
      await tester.pumpAndSettle();
      final events = (
        find.byKey(const Key('event-motion')).evaluate().length,
        find.byKey(const Key('event-person')).evaluate().length,
      );
      final icons = find
          .byKey(const Key('event-person-icon'))
          .evaluate()
          .length;

      // act
      await tester.tap(find.byKey(const Key('event-person')));
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(
        find.byKey(const Key('only-people')),
        200,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.byKey(const Key('only-people')));
      await tester.pumpAndSettle();

      // assert
      expect(events, (1, 1));
      expect(icons, 1);
      expect(
        player.plays.single.from,
        const Duration(minutes: 39, seconds: 55),
      );
      expect(
        find.text('Motion on this day: 2 · with people: 1'),
        findsOneWidget,
      );
      expect(find.text('People on this day'), findsNothing);
      expect(find.byKey(const Key('event-motion')), findsNothing);
      expect(find.byKey(const Key('event-person')), findsOneWidget);
    });

    testWidgets('segmentWithPeople_drawsBoxesThatShowPeopleTurnsOff', (
      tester,
    ) async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      final api = _noonApi(day)
        ..peopleBySegment['/api/recordings/cam/noon.mp4'] = const SegmentPeople(
          seconds: [
            PeopleSecond(
              at: 3,
              people: [PersonBox(x: 0.1, y: 0.2, width: 0.3, height: 0.4)],
            ),
          ],
        );
      await tester.pumpWidget(_screen(api, FakeRecordingPlayer()));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('timeline-hours')));
      await tester.pumpAndSettle();
      // The 30 minute window is 12:45 to 13:15; a quarter in is 12:52:30, inside the recording.
      final hours = tester.getRect(find.byKey(const Key('timeline-hours')));
      await tester.tapAt(Offset(hours.left + hours.width / 4, hours.center.dy));
      await tester.pump();
      await tester.pump();
      final drawn = find.byKey(const Key('people-boxes')).evaluate().length;

      // act
      await tester.ensureVisible(find.byKey(const Key('show-people')));
      await tester.tap(find.byKey(const Key('show-people')));
      await tester.pump();

      // assert
      expect(drawn, 1);
      expect(find.text('Show people'), findsOneWidget);
      expect(find.byKey(const Key('people-boxes')), findsNothing);
    });

    testWidgets('segmentNotAnalyzed_hasNoBoxesAndNoSwitch', (tester) async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      final api = _noonApi(day);
      await tester.pumpWidget(_screen(api, FakeRecordingPlayer()));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('timeline-hours')));
      await tester.pumpAndSettle();

      // act
      // The 30 minute window is 12:45 to 13:15; a quarter in is 12:52:30, inside the recording.
      final hours = tester.getRect(find.byKey(const Key('timeline-hours')));
      await tester.tapAt(Offset(hours.left + hours.width / 4, hours.center.dy));
      await tester.pump();
      await tester.pump();

      // assert
      expect(find.byKey(const Key('recording-video')), findsOneWidget);
      expect(find.byKey(const Key('people-boxes')), findsNothing);
      expect(find.byKey(const Key('show-people')), findsNothing);
    });

    testWidgets('notAnalyzed_showsNothingAboutPeople', (tester) async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      final api = _noonApi(day)
        ..motionByDay[day] = [
          MotionEventInfo(
            start: DateTime.utc(2026, 9, 25, 12, 30),
            end: DateTime.utc(2026, 9, 25, 12, 31),
          ),
        ];

      // act
      await tester.pumpWidget(_screen(api, FakeRecordingPlayer()));
      await tester.pumpAndSettle();

      // assert
      expect(find.byKey(const Key('event-person')), findsNothing);
      await tester.scrollUntilVisible(
        find.text('Motion on this day: 1'),
        200,
        scrollable: find.byType(Scrollable).first,
      );
      expect(find.text('Motion on this day: 1'), findsOneWidget);
      expect(find.byKey(const Key('only-people')), findsNothing);
    });

    testWidgets('analyzedWithNobody_saysNoneHadPeople', (tester) async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      final api = _noonApi(day)
        ..motionByDay[day] = [
          MotionEventInfo(
            start: DateTime.utc(2026, 9, 25, 12, 30),
            end: DateTime.utc(2026, 9, 25, 12, 31),
            person: false,
          ),
        ];

      // act
      await tester.pumpWidget(_screen(api, FakeRecordingPlayer()));
      await tester.pumpAndSettle();
      final summary = find.text('Motion on this day: 1 · with people: 0');
      await tester.scrollUntilVisible(
        summary,
        200,
        scrollable: find.byType(Scrollable).first,
      );

      // assert
      expect(summary, findsOneWidget);
    });

    testWidgets('zoomDragAndHold_moveTheCenterAndShowTheTime', (tester) async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      final api = FakeApiClient()
        ..recordingDaysResult = [day]
        ..recordingsByDay[day] = [
          RecordingPieceInfo(
            start: DateTime.utc(2026, 9, 25, 12),
            end: DateTime.utc(2026, 9, 25, 13),
            segments: [
              RecordingSegmentInfo(
                start: DateTime.utc(2026, 9, 25, 12),
                end: DateTime.utc(2026, 9, 25, 13),
                url: '/api/recordings/cam/noon.mp4',
              ),
            ],
          ),
        ]
        ..quotaResult = ApiSuccess(
          const RecordingQuota(
            megabytes: 2048,
            usedBytes: 300 * 1024 * 1024,
            freeBytes: 10 * 1024 * 1024 * 1024,
          ),
        );
      final player = FakeRecordingPlayer();
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: RecordingsTimelineScreen(
            brighten: () => BrightenController(
              store: FakeAdjustmentStore(),
              cameraId: 'cam',
            ),
            cameraName: 'Porch',
            create: () => RecordingTimelineController(
              api: api,
              session: pairedSession(),
              cameraId: 'cam',
              player: player,
              segments: FakeSegmentSource(),
              peopleBoxes: FakePeopleBoxesStore(),
              utcOffsetOf: (_) => Duration.zero,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(
        find.byKey(const Key('timeline-hours')),
        200,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.pumpAndSettle();
      String center() =>
          tester.widget<Text>(find.byKey(const Key('timeline-center'))).data!;
      final opened = center();
      await tester.tap(find.text('5 min'));
      await tester.pumpAndSettle();
      final zoomed = center();
      final bar = tester.getRect(find.byKey(const Key('timeline-hours')));

      // act
      // A fifth of the bar to the right is a minute earlier; letting go plays from there.
      await tester.drag(
        find.byKey(const Key('timeline-hours')),
        Offset(bar.width / 5, 0),
      );
      await tester.pumpAndSettle();
      final dragged = center();
      // The player opened above the bar, so it moved.
      await tester.ensureVisible(find.byKey(const Key('timeline-hours')));
      await tester.pumpAndSettle();
      final moved = tester.getRect(find.byKey(const Key('timeline-hours')));
      final gesture = await tester.startGesture(moved.center);
      await tester.pump(const Duration(seconds: 1));
      final held = find.byKey(const Key('timeline-pointer-time'));
      final shown = held.evaluate().length;
      await gesture.up();
      await tester.pumpAndSettle();

      // assert
      expect(opened, '9/25/2026 13:00:00');
      expect(zoomed, '9/25/2026 13:00:00');
      // The drag starts after the touch slop, so it lands a few seconds after 12:59.
      expect(dragged, matches(RegExp(r'^9/25/2026 12:59:\d\d$')));
      expect(
        player.plays.first.from.inSeconds,
        inInclusiveRange(59 * 60, 59 * 60 + 15),
      );
      expect(
        find.text('In use: 0.3 of 2 GB · about 6.8 h fit'),
        findsOneWidget,
      );
      expect(shown, 1);
      expect(player.plays, hasLength(2));
    });

    testWidgets('playerControls_pauseAndJumpTenSeconds', (tester) async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      final api = FakeApiClient()
        ..recordingDaysResult = [day]
        ..recordingsByDay[day] = [
          RecordingPieceInfo(
            start: DateTime.utc(2026, 9, 25, 12),
            end: DateTime.utc(2026, 9, 25, 14),
            segments: [
              RecordingSegmentInfo(
                start: DateTime.utc(2026, 9, 25, 12),
                end: DateTime.utc(2026, 9, 25, 14),
                url: '/api/recordings/cam/noon.mp4',
              ),
            ],
          ),
        ];
      final player = FakeRecordingPlayer();
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: RecordingsTimelineScreen(
            brighten: () => BrightenController(
              store: FakeAdjustmentStore(),
              cameraId: 'cam',
            ),
            cameraName: 'Porch',
            create: () => RecordingTimelineController(
              api: api,
              session: pairedSession(),
              cameraId: 'cam',
              player: player,
              segments: FakeSegmentSource(),
              peopleBoxes: FakePeopleBoxesStore(),
              utcOffsetOf: (_) => Duration.zero,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.scrollUntilVisible(
        find.byKey(const Key('timeline-hours')),
        200,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.ensureVisible(find.byKey(const Key('timeline-hours')));
      await tester.pumpAndSettle();
      final bar = tester.getRect(find.byKey(const Key('timeline-hours')));
      // The window opens at 13:45 to 14:15; a quarter in is 13:52:30, inside the recording.
      await tester.tapAt(bar.centerLeft + Offset(bar.width / 4, 0));
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('player-toggle')));
      await tester.pumpAndSettle();
      final start = player.position.value.position;

      // act
      await tester.tap(find.byKey(const Key('player-toggle')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('player-forward')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('player-sound')));
      await tester.pumpAndSettle();

      // assert
      expect(player.position.value.playing, isFalse);
      expect(player.seeks.single, start + const Duration(seconds: 10));
      expect(find.byIcon(Icons.play_arrow), findsOneWidget);
      expect(player.muted, isTrue);
      expect(find.byIcon(Icons.volume_off), findsOneWidget);
    });

    testWidgets('clock_followsThePlayerAndTheNextFile', (tester) async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      RecordingSegmentInfo file(int minute, String name) =>
          RecordingSegmentInfo(
            start: DateTime.utc(2026, 9, 25, 13, minute),
            end: DateTime.utc(2026, 9, 25, 13, minute + 1),
            url: '/api/recordings/cam/$name.mp4',
          );
      final api = FakeApiClient()
        ..recordingDaysResult = [day]
        ..recordingsByDay[day] = [
          RecordingPieceInfo(
            start: DateTime.utc(2026, 9, 25, 13, 44),
            end: DateTime.utc(2026, 9, 25, 13, 46),
            segments: [file(44, 'a'), file(45, 'b')],
          ),
        ];
      final player = FakeRecordingPlayer();
      await tester.pumpWidget(
        MaterialApp(
          locale: const Locale('pt'),
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: RecordingsTimelineScreen(
            brighten: () => BrightenController(
              store: FakeAdjustmentStore(),
              cameraId: 'cam',
            ),
            cameraName: 'Porch',
            create: () => RecordingTimelineController(
              api: api,
              session: pairedSession(),
              cameraId: 'cam',
              player: player,
              segments: FakeSegmentSource(),
              peopleBoxes: FakePeopleBoxesStore(),
              utcOffsetOf: (_) => Duration.zero,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('timeline-hours')));
      await tester.pumpAndSettle();
      final bar = tester.getRect(find.byKey(const Key('timeline-hours')));
      // The window opens at 13:31 to 14:01; 13.5 minutes in is 13:44:30.
      await tester.tapAt(bar.centerLeft + Offset(bar.width * 13.5 / 30, 0));
      await tester.pumpAndSettle();
      final opened = tester.widget<VideoClock>(find.byType(VideoClock)).time;

      // act
      await player.seekTo(const Duration(seconds: 45));
      await tester.pump();
      final moved = tester.widget<VideoClock>(find.byType(VideoClock)).time;
      player.finish();
      await tester.pumpAndSettle();

      // assert
      expect(opened, DateTime.utc(2026, 9, 25, 13, 44, 30));
      expect(moved, DateTime.utc(2026, 9, 25, 13, 44, 45));
      expect(player.plays.last.url.path, endsWith('b.mp4'));
      expect(find.text('13:45:00'), findsOneWidget);
    });

    testWidgets('playhead_staysInTheMiddleAndTheBarRunsUnderIt', (
      tester,
    ) async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      final api = FakeApiClient()
        ..recordingDaysResult = [day]
        ..recordingsByDay[day] = [
          RecordingPieceInfo(
            start: DateTime.utc(2026, 9, 25, 13, 45),
            end: DateTime.utc(2026, 9, 25, 13, 47),
            segments: [
              RecordingSegmentInfo(
                start: DateTime.utc(2026, 9, 25, 13, 45),
                end: DateTime.utc(2026, 9, 25, 13, 47),
                url: '/api/recordings/cam/b.mp4',
              ),
            ],
          ),
        ];
      final player = FakeRecordingPlayer();
      await tester.pumpWidget(
        MaterialApp(
          locale: const Locale('pt'),
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: RecordingsTimelineScreen(
            brighten: () => BrightenController(
              store: FakeAdjustmentStore(),
              cameraId: 'cam',
            ),
            cameraName: 'Porch',
            create: () => RecordingTimelineController(
              api: api,
              session: pairedSession(),
              cameraId: 'cam',
              player: player,
              segments: FakeSegmentSource(),
              peopleBoxes: FakePeopleBoxesStore(),
              utcOffsetOf: (_) => Duration.zero,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('timeline-hours')));
      await tester.pumpAndSettle();
      // One minute around the latest recording's end: 13:46:30 to 13:47:30.
      await tester.tap(find.text('1 min'));
      await tester.pumpAndSettle();
      final bar = tester.getRect(find.byKey(const Key('timeline-hours')));
      // 10 seconds in is 13:46:40.
      await tester.tapAt(bar.centerLeft + Offset(bar.width / 6, 0));
      await tester.pumpAndSettle();
      String center() =>
          tester.widget<Text>(find.byKey(const Key('timeline-center'))).data!;
      final opened = center();

      // act
      await player.seekTo(const Duration(minutes: 1, seconds: 20));
      await tester.pump();

      // assert
      // Playing did not reset the zoom the person chose.
      expect(opened, '25/09/2026 13:46:40');
      expect(center(), '25/09/2026 13:46:20');
      expect(
        tester.getRect(find.byKey(const Key('timeline-playhead'))).center.dx,
        closeTo(bar.center.dx, 1),
      );
    });

    testWidgets('recordingCamera_growsToNowAndLiveOpensIt', (tester) async {
      // arrange
      final day = DateTime.utc(2026, 9, 25);
      final api = FakeApiClient()
        ..recordingDaysResult = [day]
        ..recordingsByDay[day] = [
          RecordingPieceInfo(
            start: DateTime.utc(2026, 9, 25, 13),
            end: DateTime.utc(2026, 9, 25, 13, 5),
            segments: [
              RecordingSegmentInfo(
                start: DateTime.utc(2026, 9, 25, 13),
                end: DateTime.utc(2026, 9, 25, 13, 5),
                url: '/api/recordings/cam/a.mp4',
              ),
            ],
          ),
        ];
      var lives = 0;
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: RecordingsTimelineScreen(
            brighten: () => BrightenController(
              store: FakeAdjustmentStore(),
              cameraId: 'cam',
            ),
            cameraName: 'Porch',
            recording: true,
            onLive: () => lives++,
            create: () => RecordingTimelineController(
              api: api,
              session: pairedSession(),
              cameraId: 'cam',
              player: FakeRecordingPlayer(),
              segments: FakeSegmentSource(),
              peopleBoxes: FakePeopleBoxesStore(),
              utcOffsetOf: (_) => Duration.zero,
              now: () => DateTime.utc(2026, 9, 25, 13, 6),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('timeline-live')));
      await tester.pumpAndSettle();

      // act
      await tester.tap(find.byKey(const Key('timeline-live')));
      await tester.pump();

      // assert
      // The newest stretch still records, so the bar opens on now, not on its last file's end.
      expect(
        tester.widget<Text>(find.byKey(const Key('timeline-center'))).data,
        '9/25/2026 13:06:00',
      );
      expect(lives, 1);
      await tester.pumpWidget(const SizedBox());
    });

    testWidgets('withoutRecordings_saysHowToStart', (tester) async {
      // arrange
      final api = FakeApiClient();

      // act
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: RecordingsTimelineScreen(
            brighten: () => BrightenController(
              store: FakeAdjustmentStore(),
              cameraId: 'cam',
            ),
            cameraName: 'Porch',
            create: () => RecordingTimelineController(
              api: api,
              session: pairedSession(),
              cameraId: 'cam',
              player: FakeRecordingPlayer(),
              segments: FakeSegmentSource(),
              peopleBoxes: FakePeopleBoxesStore(),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();

      // assert
      expect(
        find.text('No recordings yet. Turn on Record always for this camera.'),
        findsOneWidget,
      );
    });
  });
}

/// One hour of recording from noon UTC, in one file.
FakeApiClient _noonApi(DateTime day) => FakeApiClient()
  ..recordingDaysResult = [day]
  ..recordingsByDay[day] = [
    RecordingPieceInfo(
      start: DateTime.utc(2026, 9, 25, 12),
      end: DateTime.utc(2026, 9, 25, 13),
      segments: [
        RecordingSegmentInfo(
          start: DateTime.utc(2026, 9, 25, 12),
          end: DateTime.utc(2026, 9, 25, 13),
          url: '/api/recordings/cam/noon.mp4',
        ),
      ],
    ),
  ];

Widget _screen(FakeApiClient api, FakeRecordingPlayer player) => MaterialApp(
  localizationsDelegates: AppLocalizations.localizationsDelegates,
  supportedLocales: AppLocalizations.supportedLocales,
  home: RecordingsTimelineScreen(
    brighten: () =>
        BrightenController(store: FakeAdjustmentStore(), cameraId: 'cam'),
    cameraName: 'Porch',
    create: () => RecordingTimelineController(
      api: api,
      session: pairedSession(),
      cameraId: 'cam',
      player: player,
      segments: FakeSegmentSource(),
      peopleBoxes: FakePeopleBoxesStore(),
      utcOffsetOf: (_) => Duration.zero,
    ),
  ),
);
