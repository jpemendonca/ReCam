import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/l10n/generated/app_localizations.dart';
import 'package:recam/viewer/brighten_controller.dart';
import 'package:recam/viewer/recording_timeline_controller.dart';
import 'package:recam/viewer/recordings_timeline_screen.dart';

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
              utcOffsetOf: (_) => Duration.zero,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      final waiting = find
          .text('Tap the hours below to watch from that moment.')
          .evaluate()
          .length;

      // act
      await tester.ensureVisible(find.byKey(const Key('timeline-hours')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('timeline-hours')));
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
              utcOffsetOf: (_) => Duration.zero,
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      await tester.ensureVisible(find.byKey(const Key('timeline-hours')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('3 h'));
      await tester.pumpAndSettle();
      final bar = tester.getRect(find.byKey(const Key('timeline-hours')));
      final mark = tester.getRect(find.byKey(const Key('motion-mark')));

      // act
      await tester.scrollUntilVisible(
        find.text('Low'),
        200,
        scrollable: find.byType(Scrollable).first,
      );
      await tester.tap(find.byKey(const Key('next-motion')));
      await tester.pumpAndSettle();
      await tester.tap(find.byKey(const Key('only-motion')));
      await tester.pumpAndSettle();
      final onlyMotion = tester.getRect(find.byKey(const Key('motion-mark')));
      await tester.tap(find.text('Low'));
      await tester.pumpAndSettle();

      // assert
      // The 3-hour window opens on the latest recording, 11:30 to 14:30.
      expect(mark.left - bar.left, closeTo(bar.width / 3, 1));
      expect(mark.height, lessThan(onlyMotion.height));
      expect(
        player.plays.single.from,
        const Duration(minutes: 29, seconds: 55),
      );
      expect(api.motionSensitivity, MotionSensitivity.low);
      expect(find.byKey(const Key('motion-mark')), findsNothing);
      expect(find.text('No motion on this day.'), findsOneWidget);
    });

    testWidgets('zoomDragAndHold_moveTheWindowAndShowTheTime', (tester) async {
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
      final opened = tester
          .widget<Text>(find.byKey(const Key('timeline-window')))
          .data;
      await tester.tap(find.text('15 min'));
      await tester.pumpAndSettle();
      final zoomed = tester
          .widget<Text>(find.byKey(const Key('timeline-window')))
          .data;
      final bar = tester.getRect(find.byKey(const Key('timeline-hours')));

      // act
      await tester.drag(
        find.byKey(const Key('timeline-hours')),
        Offset(bar.width / 3, 0),
      );
      await tester.pumpAndSettle();
      final dragged = tester
          .widget<Text>(find.byKey(const Key('timeline-window')))
          .data;
      final gesture = await tester.startGesture(bar.center);
      await tester.pump(const Duration(seconds: 1));
      final held = find.byKey(const Key('timeline-pointer-time'));
      final shown = held.evaluate().length;
      await gesture.up();
      await tester.pumpAndSettle();

      // assert
      expect(opened, '12:30 – 13:30');
      expect(zoomed, '12:52 – 13:07');
      expect(dragged, '12:47 – 13:02');
      expect(
        find.text('In use: 0.3 of 2 GB · about 6.8 h fit'),
        findsOneWidget,
      );
      expect(shown, 1);
      expect(player.plays, hasLength(1));
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
