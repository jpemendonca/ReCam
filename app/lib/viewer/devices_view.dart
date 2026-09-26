import 'dart:async';

import 'package:flutter/material.dart';

import '../core/network/api_client.dart';
import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'devices_controller.dart';

/// The Monitor's Devices tab: the cameras and Monitors on the server, and removing one.
class DevicesView extends StatefulWidget {
  const DevicesView({required this.api, required this.session, super.key});

  final ApiClient api;
  final PairedSession session;

  @override
  State<DevicesView> createState() => _DevicesViewState();
}

class _DevicesViewState extends State<DevicesView> {
  late final _controller = DevicesController(
    api: widget.api,
    session: widget.session,
  );

  @override
  void initState() {
    super.initState();
    unawaited(_controller.load());
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _remove(DeviceInfo device) async {
    final l10n = AppLocalizations.of(context);
    final messenger = ScaffoldMessenger.of(context);
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.removeDeviceTitle(device.name)),
        content: Text(l10n.removeDeviceMessage),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: Text(l10n.cancelButton),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(l10n.removeDeviceConfirm),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    if (!await _controller.remove(device)) {
      messenger.showSnackBar(SnackBar(content: Text(l10n.removeDeviceFailed)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return ListenableBuilder(
      listenable: _controller,
      builder: (context, _) => switch (_controller.state) {
        DevicesLoading() => const Center(child: CircularProgressIndicator()),
        DevicesFailed() => Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(l10n.devicesLoadFailed, textAlign: TextAlign.center),
              const SizedBox(height: 16),
              FilledButton(
                onPressed: _controller.load,
                child: Text(l10n.retryButton),
              ),
            ],
          ),
        ),
        final DevicesLoaded loaded => ListView(
          children: [
            _Header(text: l10n.devicesCameras),
            if (loaded.cameras.isEmpty)
              ListTile(title: Text(l10n.cameraListEmpty)),
            for (final device in loaded.cameras) _tile(device, l10n),
            _Header(text: l10n.devicesMonitors),
            for (final device in loaded.monitors) _tile(device, l10n),
          ],
        ),
      },
    );
  }

  Widget _tile(DeviceInfo device, AppLocalizations l10n) {
    final thisPhone = _controller.isThisPhone(device);
    return ListTile(
      leading: Icon(device.isCamera ? Icons.videocam_outlined : Icons.live_tv),
      title: Text(device.name),
      subtitle: Text(
        thisPhone
            ? l10n.devicesThisPhone
            : device.online
            ? l10n.cameraOnline
            : l10n.cameraOffline,
      ),
      trailing: thisPhone
          ? null
          : IconButton(
              tooltip: l10n.removeDeviceConfirm,
              onPressed: () => _remove(device),
              icon: const Icon(Icons.delete_outline),
            ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(16, 16, 16, 4),
    child: Text(text, style: Theme.of(context).textTheme.titleSmall),
  );
}
