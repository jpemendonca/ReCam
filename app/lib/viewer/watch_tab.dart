import 'package:flutter/material.dart';

import '../core/network/api_client.dart';
import '../core/pairing/pairing_labels.dart';
import '../core/pairing/pairing_service.dart';
import '../l10n/generated/app_localizations.dart';
import 'camera_list_controller.dart';
import 'camera_list_view.dart';
import 'viewer_pairing_controller.dart';

class WatchTab extends StatelessWidget {
  const WatchTab({
    required this.pairing,
    required this.api,
    required this.cameraList,
    required this.onScan,
    super.key,
  });

  final ViewerPairingController pairing;
  final ApiClient api;
  final CameraListFactory cameraList;

  /// Opens the app's QR reader; the code read decides which tab pairs.
  final Future<void> Function() onScan;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: pairing,
      builder: (context, _) => switch (pairing.state) {
        ViewerPairingLoading() ||
        ViewerPairing() => const Center(child: CircularProgressIndicator()),
        ViewerNotPaired(:final lastFailure) => _NotPaired(
          failure: lastFailure,
          onScan: onScan,
        ),
        ViewerPaired(:final session) => CameraListView(
          api: api,
          session: session,
          cameraList: cameraList,
          onPairingLost: pairing.forget,
        ),
      },
    );
  }
}

class _NotPaired extends StatelessWidget {
  const _NotPaired({required this.failure, required this.onScan});

  final PairingFailure? failure;
  final Future<void> Function() onScan;

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
              onPressed: onScan,
              icon: const Icon(Icons.qr_code_scanner),
              label: Text(l10n.scanQrButton),
            ),
            if (failure != null) ...[
              const SizedBox(height: 16),
              Text(
                pairingFailureText(l10n, failure),
                textAlign: TextAlign.center,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
