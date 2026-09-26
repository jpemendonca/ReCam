import 'package:flutter/material.dart';

import '../core/network/api_client.dart';
import '../core/scanner/qr_scanner_screen.dart';
import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'connect_browser_controller.dart';

/// "Connect browser": reads the QR code a browser shows and lets it in as a Monitor.
class ConnectBrowserScreen extends StatefulWidget {
  const ConnectBrowserScreen({
    required this.api,
    required this.session,
    required this.readCode,
    super.key,
  });

  final ApiClient api;
  final PairedSession session;
  final PairingCodeReader readCode;

  @override
  State<ConnectBrowserScreen> createState() => _ConnectBrowserScreenState();
}

class _ConnectBrowserScreenState extends State<ConnectBrowserScreen> {
  late final _controller = ConnectBrowserController(
    api: widget.api,
    session: widget.session,
  );

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _scan() async {
    final code = await widget.readCode(context);
    if (code == null || !mounted) return;
    await _controller.approve(code);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(l10n.connectBrowserTitle)),
      body: ListenableBuilder(
        listenable: _controller,
        builder: (context, _) {
          final state = _controller.state;
          final message = switch (state) {
            ConnectBrowserDone() => l10n.connectBrowserDone,
            ConnectBrowserWrongCode() => l10n.connectBrowserWrongCode,
            ConnectBrowserFailed(kind: ApiFailureKind.rejected) =>
              l10n.connectBrowserExpired,
            ConnectBrowserFailed() => l10n.connectBrowserFailed,
            _ => null,
          };
          return ListView(
            padding: const EdgeInsets.all(24),
            children: [
              Text(l10n.connectBrowserInstructions),
              const SizedBox(height: 24),
              if (state is ConnectBrowserApproving)
                const Center(child: CircularProgressIndicator())
              else
                FilledButton.icon(
                  onPressed: _scan,
                  icon: const Icon(Icons.qr_code_scanner),
                  label: Text(l10n.connectBrowserScan),
                ),
              if (message != null) ...[
                const SizedBox(height: 16),
                Text(
                  message,
                  key: const Key('connect-browser-message'),
                  textAlign: TextAlign.center,
                ),
              ],
            ],
          );
        },
      ),
    );
  }
}
