/// The "Connect browser" QR code a browser shows when it is not a Monitor:
/// `recam://connect-browser?v=1&l=<link id>&s=<secret>`. The phone that reads it is already
/// paired, so the code carries no server address.
class BrowserLinkCode {
  const BrowserLinkCode({required this.linkId, required this.secret});

  final String linkId;
  final String secret;

  /// Null when the text is not a "Connect browser" code of a version this app knows.
  static BrowserLinkCode? tryParse(String raw) {
    final uri = Uri.tryParse(raw.trim());
    if (uri == null ||
        uri.scheme != 'recam' ||
        uri.host != 'connect-browser' ||
        uri.queryParameters['v'] != '1') {
      return null;
    }
    final linkId = uri.queryParameters['l'] ?? '';
    final secret = uri.queryParameters['s'] ?? '';
    if (!RegExp(r'^[0-9a-fA-F]{32}$').hasMatch(linkId) || secret.isEmpty) {
      return null;
    }
    return BrowserLinkCode(linkId: linkId, secret: secret);
  }
}
