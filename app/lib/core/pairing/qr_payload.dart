enum QrError {
  notPairingUri,
  unsupportedVersion,
  missingToken,
  missingUrl,
  invalidFingerprint,
}

class QrPayload {
  const QrPayload({
    required this.token,
    required this.serverUrls,
    this.fingerprint,
  });

  static const int supportedVersion = 1;

  final String token;
  final String? fingerprint;
  final List<Uri> serverUrls;

  static final _fingerprintPattern = RegExp(r'^[0-9a-f]{64}$');

  static QrParseResult parse(String raw) {
    final uri = Uri.tryParse(raw.trim());
    if (uri == null || uri.scheme != 'recam' || uri.host != 'pair') {
      return QrParseFailed([QrError.notPairingUri]);
    }

    final query = uri.queryParametersAll;
    final errors = <QrError>[];

    if (query['v']?.firstOrNull != '$supportedVersion') {
      errors.add(QrError.unsupportedVersion);
    }

    final token = query['t']?.firstOrNull;
    if (token == null || token.isEmpty) errors.add(QrError.missingToken);

    final serverUrls = [
      for (final value in query['u'] ?? const <String>[])
        ?_parseServerUrl(value),
    ];
    if (serverUrls.isEmpty) errors.add(QrError.missingUrl);

    final fingerprint = query['f']?.firstOrNull;
    if (fingerprint != null && !_fingerprintPattern.hasMatch(fingerprint)) {
      errors.add(QrError.invalidFingerprint);
    }

    if (errors.isNotEmpty) return QrParseFailed(errors);
    return QrParseOk(
      QrPayload(
        token: token!,
        fingerprint: fingerprint,
        serverUrls: serverUrls,
      ),
    );
  }

  static Uri? _parseServerUrl(String value) {
    final url = Uri.tryParse(value);
    if (url == null || !url.hasAuthority || url.host.isEmpty) return null;
    if (url.scheme != 'https' && url.scheme != 'http') return null;
    return url;
  }
}

sealed class QrParseResult {}

final class QrParseOk extends QrParseResult {
  QrParseOk(this.payload);

  final QrPayload payload;
}

final class QrParseFailed extends QrParseResult {
  QrParseFailed(this.errors);

  final List<QrError> errors;
}
