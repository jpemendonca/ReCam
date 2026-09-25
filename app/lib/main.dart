import 'dart:io';

import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;

import 'app.dart';
import 'camera/camera_pairing_controller.dart';
import 'core/network/http_api_client.dart';
import 'core/network/pinned_http_overrides.dart';
import 'core/pairing/pairing_link.dart';
import 'core/pairing/pairing_service.dart';
import 'core/storage/secure_credential_store.dart';
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
      links: AppLinkSource(),
      ready: ready,
    ),
  );
  await ready;
}
