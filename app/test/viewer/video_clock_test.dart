import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/viewer/video_clock.dart';

void main() {
  group('LiveClock', () {
    testWidgets('showsTheTimeNowAndTicksEverySecond', (tester) async {
      // arrange
      var now = DateTime(2026, 9, 26, 21, 5, 9);
      await tester.pumpWidget(MaterialApp(home: LiveClock(now: () => now)));
      final first = find.text('21:05:09').evaluate().length;

      // act
      now = now.add(const Duration(seconds: 1));
      await tester.pump(const Duration(seconds: 1));

      // assert
      expect(first, 1);
      expect(find.text('21:05:10'), findsOneWidget);
    });
  });
}
