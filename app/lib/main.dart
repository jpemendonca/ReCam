import 'dart:io';

import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;

import 'app.dart';
import 'camera/battery_guide.dart';
import 'camera/camera_mode_controller.dart';
import 'camera/camera_pairing_controller.dart';
import 'core/device/battery_optimization.dart';
import 'core/device/battery_reader.dart';
import 'core/device/keep_alive.dart';
import 'core/device/screen_controller.dart';
import 'core/media/camera_capture.dart';
import 'core/media/recording_player.dart';
import 'core/media/recording_relay.dart';
import 'core/media/signaling_client.dart';
import 'core/media/webrtc_publisher.dart';
import 'core/media/webrtc_viewer.dart';
import 'core/network/hub_client.dart';
import 'core/network/hub_session.dart';
import 'core/network/http_api_client.dart';
import 'core/network/pinned_http_overrides.dart';
import 'core/pairing/pairing_link.dart';
import 'core/pairing/pairing_service.dart';
import 'core/storage/secure_credential_store.dart';
import 'viewer/camera_list_controller.dart';
import 'viewer/recording_timeline_controller.dart';
import 'viewer/viewer_pairing_controller.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  final pins = PinnedHttpOverrides();
  HttpOverrides.global = pins;

  final api = HttpApiClient(http.Client());
  final pairing = PairingService(
    api: api,
    store: SecureCredentialStore(),
    pins: pins,
  );
  final cameraPairing = CameraPairingController(pairing: pairing);
  final viewerPairing = ViewerPairingController(pairing: pairing);
  final ready = Future.wait([cameraPairing.load(), viewerPairing.load()]);
  runApp(
    RecamApp(
      cameraPairing: cameraPairing,
      viewerPairing: viewerPairing,
      api: api,
      cameraMode: (session) => CameraModeController(
        hub: HubSession(client: SignalRHubClient(session, pins)),
        battery: PluginBatteryReader(),
        screen: PluginScreenController(),
        keepAlive: ForegroundServiceKeepAlive(),
        capture: PluginCameraCapture(),
        publisher: WhipPublisher(
          session: session,
          signaling: SignalingClient(http.Client()),
        ),
      ),
      batteryGuide: BatteryGuideController(
        optimization: PluginBatteryOptimization(),
      ),
      cameraList: (session) => CameraListController(
        api: api,
        session: session,
        hub: HubSession(client: SignalRHubClient(session, pins)),
        viewerFactory: (cameraId) => WhepViewer(
          session: session,
          cameraId: cameraId,
          signaling: SignalingClient(http.Client()),
        ),
        timelineFactory: (cameraId) => RecordingTimelineController(
          api: api,
          session: session,
          cameraId: cameraId,
          player: VideoPlayerRecordingPlayer(),
          segments: RecordingRelay(session: session),
        ),
      ),
      links: AppLinkSource(),
      ready: ready,
    ),
  );
  await ready;
}
