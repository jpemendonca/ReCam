import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/viewer/person_track.dart';

PersonBox _box(double x, double y, double width, double height) =>
    PersonBox(x: x, y: y, width: width, height: height);

void main() {
  group('PersonTrack.at', () {
    test('betweenSeconds_interpolates', () {
      // arrange
      final track = PersonTrack(
        SegmentPeople(
          seconds: [
            PeopleSecond(at: 4, people: [_box(0.1, 0.2, 0.2, 0.4)]),
            PeopleSecond(at: 5, people: [_box(0.3, 0.2, 0.2, 0.6)]),
          ],
        ),
      );

      // act
      final box = track.at(4.5).single;

      // assert
      expect(box.x, closeTo(0.2, 1e-9));
      expect(box.height, closeTo(0.5, 1e-9));
    });

    test('nobodyNext_staysThenGoes', () {
      // arrange
      final track = PersonTrack(
        SegmentPeople(
          seconds: [
            PeopleSecond(at: 4, people: [_box(0.1, 0.2, 0.2, 0.4)]),
            const PeopleSecond(at: 5, people: []),
          ],
        ),
      );

      // act
      final during = track.at(4.9);
      final after = track.at(5.2);
      final unlooked = track.at(9);

      // assert
      expect(during, [_box(0.1, 0.2, 0.2, 0.4)]);
      expect(after, isEmpty);
      expect(unlooked, isEmpty);
    });

    test('twoPeople_eachFollowsTheNearest', () {
      // arrange
      final track = PersonTrack(
        SegmentPeople(
          seconds: [
            PeopleSecond(
              at: 0,
              people: [_box(0.1, 0.1, 0.1, 0.1), _box(0.7, 0.1, 0.1, 0.1)],
            ),
            PeopleSecond(
              at: 1,
              people: [_box(0.8, 0.1, 0.1, 0.1), _box(0.2, 0.1, 0.1, 0.1)],
            ),
          ],
        ),
      );

      // act
      final boxes = track.at(0.5);

      // assert
      expect(boxes.map((box) => box.x), [
        closeTo(0.15, 1e-9),
        closeTo(0.75, 1e-9),
      ]);
    });
  });
}
