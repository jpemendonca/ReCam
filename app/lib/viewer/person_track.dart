import 'dart:math' as math;

import '../core/network/api_client.dart';

/// Where the people are in the file playing, at any moment (SPECS.md 2.6). The detect service
/// looks once a second, so a box moves from one second's place to the next one's instead of
/// jumping; a box with nobody near it the next second stays put and then goes away. Same rules as
/// the browser's PersonTrack.
class PersonTrack {
  PersonTrack(SegmentPeople people)
    : _bySecond = {
        for (final second in people.seconds.reversed) second.at: second.people,
      };

  /// How far, in fractions of the frame, a person may move in a second and still be the same one.
  static const samePersonDistance = 0.25;

  final Map<double, List<PersonBox>> _bySecond;

  /// The boxes at [seconds] into the file.
  List<PersonBox> at(double seconds) {
    final second = seconds.floorToDouble();
    final now = _bySecond[second];
    if (now == null) return const [];
    final next = _bySecond[second + 1] ?? const [];
    final fraction = seconds - second;
    final taken = <PersonBox>{};
    return [
      for (final box in now)
        () {
          final candidates =
              next
                  .where(
                    (candidate) =>
                        !taken.contains(candidate) &&
                        _distance(box, candidate) <= samePersonDistance,
                  )
                  .toList()
                ..sort(
                  (a, b) => _distance(box, a).compareTo(_distance(box, b)),
                );
          if (candidates.isEmpty) return box;
          final match = candidates.first;
          taken.add(match);
          return PersonBox(
            x: _lerp(box.x, match.x, fraction),
            y: _lerp(box.y, match.y, fraction),
            width: _lerp(box.width, match.width, fraction),
            height: _lerp(box.height, match.height, fraction),
          );
        }(),
    ];
  }

  static double _lerp(double from, double to, double fraction) =>
      from + (to - from) * fraction;

  static double _distance(PersonBox a, PersonBox b) {
    final x = (a.x + a.width / 2) - (b.x + b.width / 2);
    final y = (a.y + a.height / 2) - (b.y + b.height / 2);
    return math.sqrt(x * x + y * y);
  }
}
