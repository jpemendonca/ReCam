import 'package:flutter/foundation.dart';

import '../core/network/api_client.dart';
import '../core/pairing/browser_link_code.dart';
import '../core/storage/credential_store.dart';

sealed class ConnectBrowserState {}

final class ConnectBrowserIdle extends ConnectBrowserState {}

final class ConnectBrowserApproving extends ConnectBrowserState {}

/// The browser got in; it shows the cameras by itself.
final class ConnectBrowserDone extends ConnectBrowserState {}

/// The code read is not a "Connect browser" QR code.
final class ConnectBrowserWrongCode extends ConnectBrowserState {}

final class ConnectBrowserFailed extends ConnectBrowserState {
  ConnectBrowserFailed(this.kind);

  final ApiFailureKind kind;
}

/// "Connect browser": reads the QR code a browser shows and lets it in as a Monitor.
class ConnectBrowserController extends ChangeNotifier {
  ConnectBrowserController({required this._api, required this._session});

  final ApiClient _api;
  final PairedSession _session;
  ConnectBrowserState _state = ConnectBrowserIdle();

  ConnectBrowserState get state => _state;

  Future<void> approve(String raw) async {
    final code = BrowserLinkCode.tryParse(raw);
    if (code == null) {
      _setState(ConnectBrowserWrongCode());
      return;
    }
    _setState(ConnectBrowserApproving());
    final failure = await _api.approveBrowserLink(
      _session.serverUrl,
      _session.credential,
      code.linkId,
      code.secret,
    );
    _setState(
      failure == null ? ConnectBrowserDone() : ConnectBrowserFailed(failure),
    );
  }

  void _setState(ConnectBrowserState state) {
    _state = state;
    notifyListeners();
  }
}
