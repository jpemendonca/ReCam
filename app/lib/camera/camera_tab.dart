import 'dart:async';

import 'package:flutter/material.dart';

import '../core/pairing/pairing_labels.dart';
import '../core/scanner/qr_scanner_screen.dart';
import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'camera_mode_controller.dart';
import 'camera_mode_screen.dart';
import 'camera_pairing_controller.dart';

class CameraTab extends StatefulWidget {
  const CameraTab({required this.pairing, required this.cameraMode, super.key});

  final CameraPairingController pairing;
  final CameraModeFactory cameraMode;

  @override
  State<CameraTab> createState() => _CameraTabState();
}

class _CameraTabState extends State<CameraTab> {
  final _nameController = TextEditingController();
  bool _nameInitialized = false;
  late CameraPairingState _previousState;

  @override
  void initState() {
    super.initState();
    _previousState = widget.pairing.state;
    widget.pairing.addListener(_openCameraModeWhenJustPaired);
  }

  // A phone paired from the Camera tab is there to be a camera: skip the "Paired" screen.
  void _openCameraModeWhenJustPaired() {
    final state = widget.pairing.state;
    final justPaired = _previousState is CameraPairing && state is CameraPaired;
    _previousState = state;
    if (justPaired && mounted) _openCameraMode(state.session);
  }

  void _openCameraMode(PairedSession session) =>
      Navigator.of(context).push<void>(
        MaterialPageRoute(
          builder: (_) => CameraModeScreen(
            create: () => widget.cameraMode(session),
            onPairingLost: () => unawaited(widget.pairing.forget()),
          ),
        ),
      );

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_nameInitialized) return;
    _nameInitialized = true;
    _nameController.text = AppLocalizations.of(context).defaultCameraName;
  }

  @override
  void dispose() {
    widget.pairing.removeListener(_openCameraModeWhenJustPaired);
    _nameController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.pairing,
      builder: (context, _) => switch (widget.pairing.state) {
        CameraPairingLoading() ||
        CameraPairing() => const Center(child: CircularProgressIndicator()),
        final CameraNotPaired state => _NotPaired(
          controller: widget.pairing,
          state: state,
          nameController: _nameController,
        ),
        CameraPaired(:final session) => _Paired(
          session: session,
          onStart: () => _openCameraMode(session),
        ),
      },
    );
  }
}

class _NotPaired extends StatelessWidget {
  const _NotPaired({
    required this.controller,
    required this.state,
    required this.nameController,
  });

  final CameraPairingController controller;
  final CameraNotPaired state;
  final TextEditingController nameController;

  Future<void> _scan(BuildContext context) async {
    final name = nameController.text;
    if (!controller.checkName(name)) return;
    final raw = await Navigator.of(
      context,
    ).push<String>(MaterialPageRoute(builder: (_) => const QrScannerScreen()));
    if (raw == null) return;
    await controller.submitQr(raw, name: name);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final failure = state.lastFailure;
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(l10n.cameraNotPaired, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            TextField(
              controller: nameController,
              decoration: InputDecoration(
                labelText: l10n.cameraNameLabel,
                border: const OutlineInputBorder(),
                errorText: state.nameErrors.isEmpty
                    ? null
                    : state.nameErrors
                          .map((error) => deviceNameErrorText(l10n, error))
                          .join('\n'),
              ),
            ),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: () => _scan(context),
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

class _Paired extends StatelessWidget {
  const _Paired({required this.session, required this.onStart});

  final PairedSession session;
  final VoidCallback onStart;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.check_circle_outline, size: 48),
          const SizedBox(height: 16),
          Text(
            l10n.watchPairedTitle(session.serverName),
            style: Theme.of(context).textTheme.titleMedium,
          ),
          const SizedBox(height: 8),
          Text(l10n.watchPairedRole(deviceRoleText(l10n, session.role))),
          const SizedBox(height: 24),
          FilledButton.icon(
            onPressed: onStart,
            icon: const Icon(Icons.videocam),
            label: Text(l10n.startCameraMode),
          ),
        ],
      ),
    );
  }
}
