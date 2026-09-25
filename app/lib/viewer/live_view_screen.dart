import 'dart:async';

import 'package:flutter/material.dart';

import '../l10n/generated/app_localizations.dart';
import 'live_view_controller.dart';

class LiveViewScreen extends StatefulWidget {
  const LiveViewScreen({
    required this.cameraName,
    required this.controller,
    super.key,
  });

  final String cameraName;
  final LiveViewController controller;

  @override
  State<LiveViewScreen> createState() => _LiveViewScreenState();
}

class _LiveViewScreenState extends State<LiveViewScreen> {
  @override
  void initState() {
    super.initState();
    unawaited(widget.controller.start());
  }

  @override
  void dispose() {
    unawaited(widget.controller.close());
    widget.controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(title: Text(widget.cameraName)),
      body: ListenableBuilder(
        listenable: widget.controller,
        builder: (context, _) => Stack(
          fit: StackFit.expand,
          children: [
            widget.controller.viewer.buildVideo(),
            switch (widget.controller.state) {
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
                        onPressed: widget.controller.retry,
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
