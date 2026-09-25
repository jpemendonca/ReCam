import 'dart:async';

import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../core/network/api_client.dart';
import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'add_camera_controller.dart';

class AddCameraScreen extends StatefulWidget {
  const AddCameraScreen({required this.api, required this.session, super.key});

  final ApiClient api;
  final PairedSession session;

  @override
  State<AddCameraScreen> createState() => _AddCameraScreenState();
}

class _AddCameraScreenState extends State<AddCameraScreen> {
  late final AddCameraController _controller;

  @override
  void initState() {
    super.initState();
    _controller = AddCameraController(api: widget.api, session: widget.session);
    unawaited(_controller.start());
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(title: Text(l10n.addCameraTitle)),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: ListenableBuilder(
            listenable: _controller,
            builder: (context, _) => switch (_controller.state) {
              AddCameraLoading() => const CircularProgressIndicator(),
              AddCameraFailed() => Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(l10n.addCameraError, textAlign: TextAlign.center),
                  const SizedBox(height: 16),
                  FilledButton(
                    onPressed: _controller.start,
                    child: Text(l10n.retryButton),
                  ),
                ],
              ),
              AddCameraReady(:final qrUri, :final remaining) => Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(l10n.addCameraInstructions, textAlign: TextAlign.center),
                  const SizedBox(height: 24),
                  QrImageView(
                    data: qrUri,
                    size: 260,
                    backgroundColor: Colors.white,
                  ),
                  const SizedBox(height: 24),
                  Text(l10n.addCameraExpiresIn(_format(remaining))),
                ],
              ),
            },
          ),
        ),
      ),
    );
  }

  static String _format(Duration duration) {
    final minutes = duration.inMinutes;
    final seconds = (duration.inSeconds % 60).toString().padLeft(2, '0');
    return '$minutes:$seconds';
  }
}
