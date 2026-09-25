import 'dart:async';

import 'package:flutter/material.dart';

import '../l10n/generated/app_localizations.dart';
import 'camera_mode_controller.dart';

/// Black full screen while the phone works as a camera. A tap shows the status and the
/// exit button for a few seconds.
class CameraModeScreen extends StatefulWidget {
  const CameraModeScreen({required this.create, super.key});

  /// Builds the controller once, in initState; the route builder may run again.
  final CameraModeController Function() create;

  @override
  State<CameraModeScreen> createState() => _CameraModeScreenState();
}

class _CameraModeScreenState extends State<CameraModeScreen> {
  static const _overlayDuration = Duration(seconds: 10);

  late final CameraModeController _controller = widget.create();

  Timer? _overlayTimer;
  bool _overlayVisible = true;
  bool _started = false;

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
    _scheduleHide();
  }

  @override
  void dispose() {
    _overlayTimer?.cancel();
    unawaited(_controller.stop());
    _controller.dispose();
    super.dispose();
  }

  void _showOverlay() {
    setState(() => _overlayVisible = true);
    _scheduleHide();
  }

  void _scheduleHide() {
    _overlayTimer?.cancel();
    _overlayTimer = Timer(_overlayDuration, () {
      if (mounted) setState(() => _overlayVisible = false);
    });
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      backgroundColor: Colors.black,
      body: GestureDetector(
        behavior: HitTestBehavior.opaque,
        onTap: _showOverlay,
        child: SizedBox.expand(
          child: !_overlayVisible
              ? const SizedBox.shrink()
              : ListenableBuilder(
                  listenable: _controller,
                  builder: (context, _) {
                    final reading = _controller.lastReading;
                    return Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            _controller.connected
                                ? Icons.cloud_done_outlined
                                : Icons.cloud_off_outlined,
                            color: Colors.white70,
                            size: 48,
                          ),
                          const SizedBox(height: 16),
                          Text(
                            _controller.connected
                                ? l10n.cameraModeConnected
                                : l10n.cameraModeConnecting,
                            style: const TextStyle(color: Colors.white70),
                          ),
                          if (reading != null) ...[
                            const SizedBox(height: 8),
                            Text(
                              l10n.batteryLevel(reading.level),
                              style: const TextStyle(color: Colors.white70),
                            ),
                          ],
                          const SizedBox(height: 16),
                          Text(
                            l10n.cameraModeHint,
                            textAlign: TextAlign.center,
                            style: const TextStyle(color: Colors.white38),
                          ),
                          const SizedBox(height: 24),
                          OutlinedButton(
                            onPressed: () => Navigator.of(context).pop(),
                            child: Text(l10n.cameraModeExit),
                          ),
                        ],
                      ),
                    );
                  },
                ),
        ),
      ),
    );
  }
}
