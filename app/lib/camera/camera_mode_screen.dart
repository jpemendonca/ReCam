import 'dart:async';

import 'package:flutter/material.dart';

import '../l10n/generated/app_localizations.dart';
import 'camera_mode_controller.dart';

/// Shown while the phone works as a camera: connection, battery and whether someone is
/// watching, plus the button to stop.
class CameraModeScreen extends StatefulWidget {
  const CameraModeScreen({
    required this.create,
    required this.onPairingLost,
    super.key,
  });

  /// Builds the controller once, in initState; the route builder may run again.
  final CameraModeController Function() create;

  /// Runs when the server refuses the pairing; the screen closes itself.
  final VoidCallback onPairingLost;

  @override
  State<CameraModeScreen> createState() => _CameraModeScreenState();
}

class _CameraModeScreenState extends State<CameraModeScreen> {
  late final CameraModeController _controller = widget.create();
  bool _started = false;
  bool _leaving = false;

  @override
  void initState() {
    super.initState();
    _controller.addListener(_leaveIfPairingLost);
  }

  void _leaveIfPairingLost() {
    if (_leaving || !_controller.pairingLost || !mounted) return;
    _leaving = true;
    widget.onPairingLost();
    Navigator.of(context).pop();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_started) return;
    _started = true;
    final l10n = AppLocalizations.of(context);
    unawaited(
      _controller.start(
        notificationTitle: l10n.cameraModeNotificationTitle,
        notificationText: l10n.cameraModeNotificationText,
      ),
    );
  }

  @override
  void dispose() {
    _controller.removeListener(_leaveIfPairingLost);
    unawaited(_controller.stop());
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(l10n.cameraTab)),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: ListenableBuilder(
            listenable: _controller,
            builder: (context, _) {
              final reading = _controller.lastReading;
              return Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    _controller.publishing
                        ? Icons.videocam
                        : _controller.connected
                        ? Icons.cloud_done_outlined
                        : Icons.cloud_off_outlined,
                    size: 64,
                    color: _controller.publishing
                        ? theme.colorScheme.error
                        : theme.colorScheme.primary,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    !_controller.connected
                        ? l10n.cameraModeConnecting
                        : _controller.publishing
                        ? l10n.cameraModeSending
                        : l10n.cameraModeConnected,
                    textAlign: TextAlign.center,
                    style: theme.textTheme.titleMedium,
                  ),
                  if (_controller.connected) ...[
                    const SizedBox(height: 8),
                    Text(
                      l10n.cameraModeWatchers(_controller.watchers),
                      textAlign: TextAlign.center,
                    ),
                  ],
                  if (_controller.torchOn) ...[
                    const SizedBox(height: 8),
                    Text(l10n.cameraModeTorchOn),
                  ],
                  if (reading != null) ...[
                    const SizedBox(height: 8),
                    Text(l10n.batteryLevel(reading.level)),
                  ],
                  const SizedBox(height: 32),
                  FilledButton.tonalIcon(
                    onPressed: () => Navigator.of(context).pop(),
                    icon: const Icon(Icons.stop),
                    label: Text(l10n.cameraModeExit),
                  ),
                ],
              );
            },
          ),
        ),
      ),
    );
  }
}
