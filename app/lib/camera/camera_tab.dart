import 'dart:async';

import 'package:flutter/material.dart';

import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'battery_guide.dart';
import 'battery_guide_screen.dart';
import 'camera_mode_controller.dart';
import 'camera_mode_screen.dart';
import 'camera_pairing_controller.dart';

class CameraTab extends StatefulWidget {
  const CameraTab({
    required this.pairing,
    required this.cameraMode,
    required this.batteryGuide,
    super.key,
  });

  final CameraPairingController pairing;
  final CameraModeFactory cameraMode;
  final BatteryGuideController batteryGuide;

  @override
  State<CameraTab> createState() => _CameraTabState();
}

class _CameraTabState extends State<CameraTab> {
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
    if (justPaired && mounted) unawaited(_openCameraMode(state.session));
  }

  // A phone with a battery setting still holding the camera back sees the guide first; one
  // with everything in place goes straight to camera mode.
  Future<void> _openCameraMode(PairedSession session) async {
    final navigator = Navigator.of(context);
    final status = await widget.batteryGuide.status();
    if (!status.allGood) {
      await navigator.push<void>(
        MaterialPageRoute(
          builder: (_) => BatteryGuideScreen(controller: widget.batteryGuide),
        ),
      );
    }
    if (!mounted) return;
    await navigator.push<void>(
      MaterialPageRoute(
        builder: (_) => CameraModeScreen(
          create: () => widget.cameraMode(session),
          onPairingLost: () => unawaited(widget.pairing.forget()),
        ),
      ),
    );
  }

  @override
  void dispose() {
    widget.pairing.removeListener(_openCameraModeWhenJustPaired);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: widget.pairing,
      builder: (context, _) => switch (widget.pairing.state) {
        CameraPairingLoading() ||
        CameraPairing() => const Center(child: CircularProgressIndicator()),
        // The shell shows the first-run screen instead while nothing is paired.
        CameraNotPaired() => const SizedBox.shrink(),
        CameraPaired(:final session) => _Paired(
          session: session,
          onStart: () => _openCameraMode(session),
        ),
      },
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
