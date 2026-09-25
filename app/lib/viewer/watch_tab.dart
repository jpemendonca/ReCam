import 'package:flutter/material.dart';

import '../core/pairing/device_role.dart';
import '../core/pairing/pairing_service.dart';
import '../core/scanner/qr_scanner_screen.dart';
import '../l10n/generated/app_localizations.dart';
import 'viewer_pairing_controller.dart';

class WatchTab extends StatelessWidget {
  const WatchTab({required this.pairing, super.key});

  final ViewerPairingController pairing;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: pairing,
      builder: (context, _) => switch (pairing.state) {
        ViewerPairingLoading() ||
        ViewerPairing() => const Center(child: CircularProgressIndicator()),
        ViewerNotPaired(:final lastFailure) => _NotPaired(
          controller: pairing,
          failure: lastFailure,
        ),
        ViewerPaired(:final session) => _Paired(
          serverName: session.serverName,
          role: session.role,
        ),
      },
    );
  }
}

class _NotPaired extends StatelessWidget {
  const _NotPaired({required this.controller, required this.failure});

  final ViewerPairingController controller;
  final PairingFailure? failure;

  Future<void> _scan(BuildContext context) async {
    final deviceName = AppLocalizations.of(context).ownerDeviceName;
    final raw = await Navigator.of(
      context,
    ).push<String>(MaterialPageRoute(builder: (_) => const QrScannerScreen()));
    if (raw == null) return;
    await controller.submitQr(raw, deviceName: deviceName);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final failure = this.failure;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(l10n.watchNotPaired, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: () => _scan(context),
              icon: const Icon(Icons.qr_code_scanner),
              label: Text(l10n.scanQrButton),
            ),
            if (failure != null) ...[
              const SizedBox(height: 16),
              Text(
                _failureText(l10n, failure),
                textAlign: TextAlign.center,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            ],
          ],
        ),
      ),
    );
  }

  static String _failureText(AppLocalizations l10n, PairingFailure failure) =>
      switch (failure) {
        PairingFailure.invalidQr => l10n.pairingErrorInvalidQr,
        PairingFailure.serverUnreachable => l10n.pairingErrorUnreachable,
        PairingFailure.tokenRejected => l10n.pairingErrorTokenRejected,
        PairingFailure.wrongRole => l10n.pairingErrorWrongRole,
        PairingFailure.invalidInput ||
        PairingFailure.unexpected => l10n.pairingErrorUnexpected,
      };
}

class _Paired extends StatelessWidget {
  const _Paired({required this.serverName, required this.role});

  final String serverName;
  final DeviceRole role;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final roleName = switch (role) {
      DeviceRole.owner => l10n.roleOwner,
      DeviceRole.viewer => l10n.roleViewer,
      DeviceRole.camera => l10n.roleCamera,
    };
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.check_circle_outline, size: 48),
          const SizedBox(height: 16),
          Text(
            l10n.watchPairedTitle(serverName),
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: 8),
          Text(l10n.watchPairedRole(roleName)),
        ],
      ),
    );
  }
}
