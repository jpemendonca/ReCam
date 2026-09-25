import 'dart:async';

import 'package:flutter/material.dart';

import '../l10n/generated/app_localizations.dart';
import 'live_view_controller.dart';

class LiveViewScreen extends StatefulWidget {
  const LiveViewScreen({
    required this.cameraName,
    required this.create,
    this.recordingSwitch,
    super.key,
  });

  final String cameraName;

  /// "Record always", shown in the app bar.
  final Widget? recordingSwitch;

  /// Builds the controller once, in initState; the route builder may run again.
  final LiveViewController Function() create;

  @override
  State<LiveViewScreen> createState() => _LiveViewScreenState();
}

class _LiveViewScreenState extends State<LiveViewScreen> {
  late final LiveViewController _controller = widget.create();

  @override
  void initState() {
    super.initState();
    unawaited(_controller.start());
  }

  @override
  void dispose() {
    unawaited(_controller.close());
    _controller.dispose();
    super.dispose();
  }

  Future<void> _switchTorch(bool on) async {
    final messenger = ScaffoldMessenger.of(context);
    final failed = AppLocalizations.of(context).torchFailed;
    if (!await _controller.setTorch(on)) {
      messenger.showSnackBar(SnackBar(content: Text(failed)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        title: Text(widget.cameraName),
        actions: [
          ?widget.recordingSwitch,
          ListenableBuilder(
            listenable: _controller,
            builder: (context, _) {
              final on = _controller.torchOn;
              return IconButton(
                tooltip: on ? l10n.torchTurnOff : l10n.torchTurnOn,
                isSelected: on,
                onPressed: _controller.state is LivePlaying
                    ? () => _switchTorch(!on)
                    : null,
                icon: const Icon(Icons.flashlight_off_outlined),
                selectedIcon: const Icon(Icons.flashlight_on),
              );
            },
          ),
        ],
      ),
      body: ListenableBuilder(
        listenable: _controller,
        builder: (context, _) => Stack(
          fit: StackFit.expand,
          children: [
            _controller.viewer.buildVideo(),
            switch (_controller.state) {
              LivePlaying() => const SizedBox.shrink(),
              LiveConnecting() => Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const CircularProgressIndicator(),
                    const SizedBox(height: 16),
                    Text(
                      l10n.liveConnecting,
                      style: const TextStyle(color: Colors.white70),
                    ),
                  ],
                ),
              ),
              LiveFailed() => Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        l10n.liveFailed,
                        textAlign: TextAlign.center,
                        style: const TextStyle(color: Colors.white70),
                      ),
                      const SizedBox(height: 16),
                      FilledButton(
                        onPressed: _controller.retry,
                        child: Text(l10n.retryButton),
                      ),
                    ],
                  ),
                ),
              ),
            },
          ],
        ),
      ),
    );
  }
}
