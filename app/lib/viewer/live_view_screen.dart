import 'dart:async';

import 'package:flutter/material.dart';

import '../l10n/generated/app_localizations.dart';
import 'brighten_controller.dart';
import 'brighten_panel.dart';
import 'live_view_controller.dart';

class LiveViewScreen extends StatefulWidget {
  const LiveViewScreen({
    required this.cameraName,
    required this.create,
    required this.brighten,
    this.recordingSwitch,
    this.recordingsButton,
    super.key,
  });

  final String cameraName;

  /// "Record always", shown in the app bar.
  final Widget? recordingSwitch;

  /// Opens this camera's recordings.
  final Widget? recordingsButton;

  /// Builds the controller once, in initState; the route builder may run again.
  final LiveViewController Function() create;

  /// This camera's "Brighten" setting on this phone.
  final BrightenController Function() brighten;

  @override
  State<LiveViewScreen> createState() => _LiveViewScreenState();
}

class _LiveViewScreenState extends State<LiveViewScreen> {
  late final LiveViewController _controller = widget.create();
  late final BrightenController _brighten = widget.brighten();

  @override
  void initState() {
    super.initState();
    unawaited(_controller.start());
    unawaited(_brighten.load());
  }

  @override
  void dispose() {
    unawaited(_controller.close());
    _controller.dispose();
    _brighten.dispose();
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
          ?widget.recordingsButton,
          BrightenButton(controller: _brighten),
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
            BrightenedVideo(
              controller: _brighten,
              child: _controller.viewer.buildVideo(),
            ),
            ListenableBuilder(
              listenable: _brighten,
              builder: (context, _) => _brighten.open
                  ? Align(
                      alignment: Alignment.bottomCenter,
                      child: BrightenPanel(controller: _brighten),
                    )
                  : const SizedBox.shrink(),
            ),
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
