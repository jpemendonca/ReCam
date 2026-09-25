import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:qr_flutter/qr_flutter.dart';

import '../core/network/api_client.dart';
import '../core/pairing/device_role.dart';
import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'add_device_controller.dart';

/// "Add": a QR code for another camera or for another phone to watch.
class AddDeviceScreen extends StatefulWidget {
  const AddDeviceScreen({
    required this.api,
    required this.session,
    this.initialRole = DeviceRole.camera,
    super.key,
  });

  final ApiClient api;
  final PairedSession session;
  final DeviceRole initialRole;

  @override
  State<AddDeviceScreen> createState() => _AddDeviceScreenState();
}

class _AddDeviceScreenState extends State<AddDeviceScreen> {
  late AddDeviceController _controller;

  @override
  void initState() {
    super.initState();
    _controller = _create(widget.initialRole);
  }

  AddDeviceController _create(DeviceRole role) {
    final controller = AddDeviceController(
      api: widget.api,
      session: widget.session,
      role: role,
    );
    unawaited(controller.start());
    return controller;
  }

  void _select(DeviceRole role) {
    if (role == _controller.role) return;
    _controller.dispose();
    setState(() => _controller = _create(role));
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
      appBar: AppBar(title: Text(l10n.addDeviceTitle)),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              SegmentedButton<DeviceRole>(
                segments: [
                  ButtonSegment(
                    value: DeviceRole.camera,
                    icon: const Icon(Icons.videocam_outlined),
                    label: Text(l10n.addDeviceCamera),
                  ),
                  ButtonSegment(
                    value: DeviceRole.viewer,
                    icon: const Icon(Icons.live_tv_outlined),
                    label: Text(l10n.addDeviceViewer),
                  ),
                ],
                selected: {_controller.role},
                onSelectionChanged: (selection) => _select(selection.single),
              ),
              const SizedBox(height: 24),
              ListenableBuilder(
                listenable: _controller,
                builder: (context, _) => switch (_controller.state) {
                  AddDeviceLoading() => const CircularProgressIndicator(),
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
                        _controller.role == DeviceRole.camera
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
            ],
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
