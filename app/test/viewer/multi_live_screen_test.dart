import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/hub_session.dart';
import 'package:recam/l10n/generated/app_localizations.dart';
import 'package:recam/viewer/live_view_controller.dart';
import 'package:recam/viewer/multi_live_screen.dart';

import '../support/fakes.dart';

void main() {
  group('MultiLiveScreen', () {
    testWidgets('withFiveCameras_watchesOnlyTheFirstFourAndStopsOnLeaving', (
      tester,
    ) async {
      // arrange
      final client = FakeHubClient();
      final hub = HubSession(client: client, delay: (_) async {});
      final cameras = [
        for (final name in ['A', 'B', 'C', 'D', 'E'])
          CameraInfo(id: name, name: name, online: true, publishing: false),
      ];

      // act
      await tester.pumpWidget(
        MaterialApp(
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: MultiLiveScreen(
            cameras: cameras,
            create: (id) => LiveViewController(
              hub: hub,
              viewer: FakeViewer(),
              cameraId: id,
              delay: (_) async {},
            ),
          ),
        ),
      );
      await tester.pump();
      final watched = [
        for (final call in client.invocations)
          if (call.method == 'WatchCamera') call.args.single,
      ];
      final fifthShown = find.text('E').evaluate().length;
      await tester.pumpWidget(const SizedBox());
      await tester.pump();

      // assert
      expect(watched, ['A', 'B', 'C', 'D']);
      expect(fifthShown, 0);
      expect(
        [
          for (final call in client.invocations)
            if (call.method == 'UnwatchCamera') call.args.single,
        ],
        ['A', 'B', 'C', 'D'],
      );
    });
  });
}
