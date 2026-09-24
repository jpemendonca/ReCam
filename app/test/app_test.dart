import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/app.dart';

void main() {
  group('RecamApp', () {
    testWidgets('whenTappingWatchTab_showsWatchPlaceholder', (tester) async {
      // arrange
      await tester.pumpWidget(const RecamApp());
      await tester.pumpAndSettle();

      // act
      await tester.tap(find.byIcon(Icons.live_tv_outlined));
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Watch your cameras from this phone.'), findsOneWidget);
    });

    testWidgets('onStart_showsCameraTab', (tester) async {
      // arrange
      await tester.pumpWidget(const RecamApp());

      // act
      await tester.pumpAndSettle();

      // assert
      expect(find.text('Use this phone as a camera.'), findsOneWidget);
    });
  });
}
