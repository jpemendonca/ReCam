import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/hub_session.dart';
import 'package:recam/l10n/generated/app_localizations.dart';
import 'package:recam/viewer/camera_list_controller.dart';
import 'package:recam/viewer/recording_switch.dart';

import '../support/fakes.dart';

void main() {
  late FakeApiClient api;
  late FakeHubClient hubClient;
  late CameraListController list;

  setUp(() {
    api = FakeApiClient();
    hubClient = FakeHubClient()..invokeResult = <String, Object?>{'ok': true};
    list = CameraListController(
      api: api,
      session: pairedSession(),
      hub: HubSession(client: hubClient, delay: (_) async {}),
      viewerFactory: (_) => FakeViewer(),
    );
  });

  Future<void> show(WidgetTester tester, CameraInfo camera) async {
    api.cameraResults.add(ApiSuccess([camera]));
    await list.refresh();
    await tester.pumpWidget(
      MaterialApp(
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: Scaffold(
          body: RecordingSwitch(list: list, cameraId: camera.id),
        ),
      ),
    );
  }

  group('RecordingSwitch', () {
    testWidgets('onTap_asksToRecordAlways', (tester) async {
      // arrange
      await show(
        tester,
        const CameraInfo(
          id: 'a',
          name: 'Porch',
          online: true,
          publishing: false,
        ),
      );

      // act
      await tester.tap(find.byType(Switch));
      await tester.pump();

      // assert
      expect(find.text('Record always'), findsOneWidget);
      expect(hubClient.invocations.single.args, ['a', true]);
    });

    testWidgets('forCameraWithoutH264_isOffWithTheReason', (tester) async {
      // arrange
      await show(
        tester,
        const CameraInfo(
          id: 'a',
          name: 'Porch',
          online: true,
          publishing: false,
          canRecord: false,
        ),
      );

      // act
      await tester.tap(find.byType(Switch));
      await tester.pump();

      // assert
      expect(
        find.text("Can't record: this phone has no H.264"),
        findsOneWidget,
      );
      expect(tester.widget<Switch>(find.byType(Switch)).onChanged, isNull);
      expect(hubClient.invocations, isEmpty);
    });
  });
}
