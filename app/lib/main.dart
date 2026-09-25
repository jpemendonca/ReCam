import 'dart:io';

import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;

import 'app.dart';
import 'core/network/http_api_client.dart';
import 'core/network/pinned_http_overrides.dart';
import 'core/pairing/pairing_service.dart';
import 'core/storage/secure_credential_store.dart';
import 'viewer/viewer_pairing_controller.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  final pins = PinnedHttpOverrides();
  HttpOverrides.global = pins;

  final pairing = PairingService(
    api: HttpApiClient(http.Client()),
    store: SecureCredentialStore(),
    pins: pins,
  );
  final viewerPairing = ViewerPairingController(pairing: pairing);
  runApp(RecamApp(viewerPairing: viewerPairing));
  await viewerPairing.load();
}
