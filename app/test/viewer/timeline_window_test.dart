import 'package:flutter_test/flutter_test.dart';
import 'package:recam/viewer/timeline_window.dart';

void main() {
  final day = DateTime.utc(2026, 9, 25);

  group('TimelineWindow.around', () {
    test('spreadsTheSpanEvenlyAroundTheCenter', () {
      // arrange
      final center = DateTime.utc(2026, 9, 25, 11, 15);

      // act
      final window = TimelineWindow.around(day, TimelineZoom.halfHour, center);

      // assert
      expect(window.start, DateTime.utc(2026, 9, 25, 11));
      expect(window.end, DateTime.utc(2026, 9, 25, 11, 30));
      expect(window.xOf(center, 400), 200);
    });

    test('nearMidnight_keepsTheCenterInTheDayAndReachesPastIt', () {
      // arrange
      final before = DateTime.utc(2026, 9, 24, 23, 40);
      final late = DateTime.utc(2026, 9, 25, 23, 54);

      // act
      final first = TimelineWindow.around(day, TimelineZoom.hour, before);
      final last = TimelineWindow.around(day, TimelineZoom.hour, late);

      // assert
      expect(first.center, day);
      expect(first.start, DateTime.utc(2026, 9, 24, 23, 30));
      expect(last.end, DateTime.utc(2026, 9, 26, 0, 24));
    });
  });

  group('TimelineWindow.timeAt', () {
    for (final (zoom, expected) in [
      (TimelineZoom.minute, DateTime.utc(2026, 9, 25, 12, 0, 15)),
      (TimelineZoom.fiveMinutes, DateTime.utc(2026, 9, 25, 12, 1, 15)),
      (TimelineZoom.halfHour, DateTime.utc(2026, 9, 25, 12, 7, 30)),
      (TimelineZoom.hour, DateTime.utc(2026, 9, 25, 12, 15)),
      (TimelineZoom.day, DateTime.utc(2026, 9, 25, 18)),
    ]) {
      test('${zoom.name}_threeQuartersIn_isAQuarterSpanAfterTheCenter', () {
        // arrange
        final window = TimelineWindow.around(
          day,
          zoom,
          DateTime.utc(2026, 9, 25, 12),
        );

        // act
        final time = window.timeAt(300, 400);

        // assert
        expect(time, expected);
        expect(window.xOf(time, 400), closeTo(300, 0.001));
      });
    }
  });

  group('TimelineWindow.panBy', () {
    test('draggingRight_showsEarlierTimes_neverBeforeMidnight', () {
      // arrange
      final window = TimelineWindow.around(
        day,
        TimelineZoom.hour,
        DateTime.utc(2026, 9, 25, 12, 30),
      );

      // act
      final dragged = window.panBy(100, 400);
      final tooFar = window.panBy(40000, 400);

      // assert
      expect(dragged.center, DateTime.utc(2026, 9, 25, 12, 15));
      expect(tooFar.center, day);
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

  group('TimelineWindow.ticks', () {
    test('fallOnRoundTimesInsideTheWindowAndTheDay', () {
      // arrange
      final window = TimelineWindow.around(
        day,
        TimelineZoom.hour,
        DateTime.utc(2026, 9, 25, 14, 35),
      );
      final late = TimelineWindow.around(
        day,
        TimelineZoom.hour,
        DateTime.utc(2026, 9, 25, 23, 50),
      );

      // act
      final ticks = window.ticks;

      // assert
      expect(ticks.first, DateTime.utc(2026, 9, 25, 14, 10));
      expect(ticks.last, DateTime.utc(2026, 9, 25, 15));
      expect(ticks, hasLength(6));
      expect(late.ticks.last, DateTime.utc(2026, 9, 26));
    });
  });
}
