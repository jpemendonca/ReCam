import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../core/network/api_client.dart';
import '../core/pairing/device_role.dart';
import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'add_device_controller.dart';

/// The QR code that pairs another phone as [role]: a camera from the "+", a Monitor from the
/// app menu. Closes with `true` once the other phone has paired.
class AddDeviceScreen extends StatefulWidget {
  const AddDeviceScreen({
    required this.api,
    required this.session,
    required this.role,
    super.key,
  });

  final ApiClient api;
  final PairedSession session;
  final DeviceRole role;

  @override
  State<AddDeviceScreen> createState() => _AddDeviceScreenState();
}

class _AddDeviceScreenState extends State<AddDeviceScreen> {
  late final AddDeviceController _controller;

  @override
  void initState() {
    super.initState();
    _controller = AddDeviceController(
      api: widget.api,
      session: widget.session,
      role: widget.role,
    );
    _controller.addListener(_closeWhenPaired);
    unawaited(_controller.start());
  }

  void _closeWhenPaired() {
    if (_controller.state is AddDevicePaired && mounted) {
      Navigator.of(context).pop(true);
    }
  }

  @override
  void dispose() {
    _controller.removeListener(_closeWhenPaired);
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final forCamera = widget.role == DeviceRole.camera;
    return Scaffold(
      appBar: AppBar(
        title: Text(forCamera ? l10n.addCameraTitle : l10n.addMonitorTitle),
      ),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ListenableBuilder(
            listenable: _controller,
            builder: (context, _) => switch (_controller.state) {
              AddDeviceLoading() ||
              AddDevicePaired() => const CircularProgressIndicator(),
              AddDeviceFailed() => Column(
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
              AddDeviceReady(:final qrUri, :final remaining) => Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    forCamera
                        ? l10n.addCameraInstructions
                        : l10n.addViewerInstructions,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 24),
                  QrImageView(
                    data: qrUri,
                    size: 260,
                    backgroundColor: Colors.white,
                  ),
                  const SizedBox(height: 24),
                  Text(l10n.addCameraExpiresIn(_format(remaining))),
                  const SizedBox(height: 8),
                  TextButton.icon(
                    onPressed: () => _copyCode(context, qrUri),
                    icon: const Icon(Icons.copy),
                    label: Text(l10n.copyCodeButton),
                  ),
                ],
              ),
            },
          ),
        ),
      ),
    );
  }

  static Future<void> _copyCode(BuildContext context, String code) async {
    final messenger = ScaffoldMessenger.of(context);
    final copied = AppLocalizations.of(context).codeCopied;
    await Clipboard.setData(ClipboardData(text: code));
    messenger.showSnackBar(SnackBar(content: Text(copied)));
  }

  static String _format(Duration duration) {
    final minutes = duration.inMinutes;
    final seconds = (duration.inSeconds % 60).toString().padLeft(2, '0');
    return '$minutes:$seconds';
  }
}
