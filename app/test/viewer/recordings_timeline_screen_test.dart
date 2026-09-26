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
