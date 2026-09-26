import 'package:flutter_test/flutter_test.dart';
import 'package:recam/viewer/timeline_window.dart';

void main() {
  final day = DateTime.utc(2026, 9, 25);

  group('TimelineWindow.around', () {
    test('centersTheWindowOnTheTime', () {
      // arrange
      final center = DateTime.utc(2026, 9, 25, 14, 30);

      // act
      final window = TimelineWindow.around(day, TimelineZoom.hour, center);

      // assert
      expect(window.start, DateTime.utc(2026, 9, 25, 14));
      expect(window.end, DateTime.utc(2026, 9, 25, 15));
    });

    test('nearMidnight_staysInsideTheDay', () {
      // arrange
      final early = DateTime.utc(2026, 9, 25, 0, 20);
      final late = DateTime.utc(2026, 9, 25, 23, 50);

      // act
      final first = TimelineWindow.around(day, TimelineZoom.threeHours, early);
      final last = TimelineWindow.around(day, TimelineZoom.threeHours, late);

      // assert
      expect(first.start, day);
      expect(last.end, DateTime.utc(2026, 9, 26));
      expect(first.atDayStart && last.atDayEnd, isTrue);
    });
  });

  group('TimelineWindow.timeAt', () {
    for (final (zoom, expected) in [
      (TimelineZoom.minute, DateTime.utc(2026, 9, 25, 12, 0, 15)),
      (TimelineZoom.quarterHour, DateTime.utc(2026, 9, 25, 12, 3, 45)),
      (TimelineZoom.hour, DateTime.utc(2026, 9, 25, 12, 15)),
      (TimelineZoom.threeHours, DateTime.utc(2026, 9, 25, 12, 45)),
    ]) {
      test('quarterOfTheBar_${zoom.name}_isAQuarterOfTheSpan', () {
        // arrange
        final window = TimelineWindow.around(
          day,
          zoom,
          DateTime.utc(2026, 9, 25, 12).add(zoom.span ~/ 2),
        );

        // act
        final time = window.timeAt(100, 400);

        // assert
        expect(time, expected);
        expect(window.xOf(time, 400), closeTo(100, 0.001));
      });
    }
  });

  group('TimelineWindow.panBy', () {
    test('draggingRight_showsEarlierTimes', () {
      // arrange
      final window = TimelineWindow.around(
        day,
        TimelineZoom.hour,
        DateTime.utc(2026, 9, 25, 12, 30),
      );

      // act
      final dragged = window.panBy(100, 400);

      // assert
      expect(dragged.start, DateTime.utc(2026, 9, 25, 11, 45));
    });

    test('pastMidnight_stopsAtTheDayStart', () {
      // arrange
      final window = TimelineWindow.around(
        day,
        TimelineZoom.hour,
        DateTime.utc(2026, 9, 25, 0, 40),
      );

      // act
      final dragged = window.panBy(4000, 400);

      // assert
      expect(dragged.start, day);
    });
  });

  group('TimelineWindow.zoomTo', () {
    test('keepsTheCenter', () {
      // arrange
      final window = TimelineWindow.around(
        day,
        TimelineZoom.hour,
        DateTime.utc(2026, 9, 25, 9, 30),
      );

      // act
      final zoomed = window.zoomTo(TimelineZoom.minute);

      // assert
      expect(zoomed.center, DateTime.utc(2026, 9, 25, 9, 30));
      expect(zoomed.end.difference(zoomed.start), const Duration(minutes: 1));
    });
  });

  group('TimelineWindow.step', () {
    test('movesOneWholeWindow', () {
      // arrange
      final window = TimelineWindow.around(
        day,
        TimelineZoom.quarterHour,
        DateTime.utc(2026, 9, 25, 10, 7, 30),
      );

      // act
      final later = window.step(1);

      // assert
      expect(later.start, DateTime.utc(2026, 9, 25, 10, 15));
    });
  });

  group('TimelineWindow.ticks', () {
    test('fallOnRoundTimesInsideTheWindow', () {
      // arrange
      final window = TimelineWindow.around(
        day,
        TimelineZoom.hour,
        DateTime.utc(2026, 9, 25, 14, 35),
      );

      // act
      final ticks = window.ticks;

      // assert
      expect(ticks.first, DateTime.utc(2026, 9, 25, 14, 10));
      expect(ticks.last, DateTime.utc(2026, 9, 25, 15));
      expect(ticks, hasLength(6));
    });
  });
}
