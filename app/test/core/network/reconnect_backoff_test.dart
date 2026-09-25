import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/reconnect_backoff.dart';

void main() {
  group('ReconnectBackoff.next', () {
    test('onRepeatedFailures_followsSpecSequenceAndStaysAt30', () {
      // arrange
      final backoff = ReconnectBackoff();

      // act
      final delays = [for (var i = 0; i < 8; i++) backoff.next().inSeconds];

      // assert
      expect(delays, [1, 2, 4, 8, 16, 30, 30, 30]);
    });

    test('afterReset_startsAgainAtOneSecond', () {
      // arrange
      final backoff = ReconnectBackoff()
        ..next()
        ..next();

      // act
      backoff.reset();

      // assert
      expect(backoff.next(), const Duration(seconds: 1));
    });
  });
}
