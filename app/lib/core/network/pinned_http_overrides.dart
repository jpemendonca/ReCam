import 'dart:io';

import 'package:crypto/crypto.dart';

/// Accepts a self-signed certificate only for hosts whose fingerprint was pinned at pairing.
/// Every other host keeps the default validation.
class PinnedHttpOverrides extends HttpOverrides {
  final Map<String, String> _pins = {};
  final Set<String> _changed = {};

  void pin(Uri serverUrl, String fingerprint) {
    final key = _key(serverUrl.host, serverUrl.port);
    _pins[key] = fingerprint.toLowerCase();
    _changed.remove(key);
  }

  bool accepts(X509Certificate certificate, String host, int port) {
    final key = _key(host, port);
    final pinned = _pins[key];
    if (pinned == null) return false;
    final matches = sha256.convert(certificate.der).toString() == pinned;
    if (matches) {
      _changed.remove(key);
    } else {
      _changed.add(key);
    }
    return matches;
  }

  /// Whether the last certificate this pinned server presented was not the pinned one. A
  /// reinstalled server has a new certificate, and the old pairing can never work again.
  bool certificateChanged(Uri serverUrl) =>
      _changed.contains(_key(serverUrl.host, serverUrl.port));

  @override
  HttpClient createHttpClient(SecurityContext? context) {
    return super.createHttpClient(context)..badCertificateCallback = accepts;
  }

  static String _key(String host, int port) => '$host:$port';
}
