import 'dart:io';

import 'package:crypto/crypto.dart';

/// Accepts a self-signed certificate only for hosts whose fingerprint was pinned at pairing.
/// Every other host keeps the default validation.
class PinnedHttpOverrides extends HttpOverrides {
  final Map<String, String> _pins = {};

  void pin(Uri serverUrl, String fingerprint) {
    _pins[_key(serverUrl.host, serverUrl.port)] = fingerprint.toLowerCase();
  }

  bool accepts(X509Certificate certificate, String host, int port) {
    final pinned = _pins[_key(host, port)];
    if (pinned == null) return false;
    return sha256.convert(certificate.der).toString() == pinned;
  }

  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return super.createHttpClient(context)..badCertificateCallback = accepts;
  }

  static String _key(String host, int port) => '$host:$port';
}
