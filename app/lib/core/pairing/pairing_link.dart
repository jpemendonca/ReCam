import 'package:app_links/app_links.dart';

import 'device_role.dart';
import 'qr_payload.dart';

enum PairingLinkTarget { camera, viewer }

/// Picks the tab that should pair with a `recam://pair` link, or null when the
/// link is not a pairing link. Links without a role hint go to the viewer tab.
PairingLinkTarget? pairingLinkTarget(String link) {
  final parsed = QrPayload.parse(link);
  if (parsed is! QrParseOk) return null;
  return parsed.payload.role == DeviceRole.camera
      ? PairingLinkTarget.camera
      : PairingLinkTarget.viewer;
}

abstract interface class LinkSource {
  /// Links that opened the app, starting with the one that launched it.
  Stream<Uri> get links;
}

class AppLinkSource implements LinkSource {
  final _appLinks = AppLinks();

  @override
  Stream<Uri> get links => _appLinks.uriLinkStream;
}
