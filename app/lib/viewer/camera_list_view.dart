import 'dart:async';

import 'package:flutter/material.dart';

import '../core/network/api_client.dart';
import '../core/pairing/pairing_labels.dart';
import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'add_device_screen.dart';
import 'camera_list_controller.dart';
import 'live_view_screen.dart';

/// The Watch tab once paired: the cameras, live, and "Add camera" for the owner.
class CameraListView extends StatefulWidget {
  const CameraListView({
    required this.api,
    required this.session,
    required this.cameraList,
    required this.onPairingLost,
    super.key,
  });

  final ApiClient api;
  final PairedSession session;
  final CameraListFactory cameraList;

  /// Runs once when the server refuses this pairing.
  final Future<void> Function() onPairingLost;

  @override
  State<CameraListView> createState() => _CameraListViewState();
}

class _CameraListViewState extends State<CameraListView> {
  late final CameraListController _controller;
  bool _pairingLostReported = false;

  @override
  void initState() {
    super.initState();
    _controller = widget.cameraList(widget.session);
    _controller.addListener(_reportPairingLost);
    unawaited(_controller.start());
  }

  // Closes a live view left on top, so the Watch tab can show the pairing screen.
  void _reportPairingLost() {
    if (_pairingLostReported || !_controller.pairingLost || !mounted) return;
    _pairingLostReported = true;
    Navigator.of(context).popUntil((route) => route.isFirst);
    unawaited(widget.onPairingLost());
  }

  @override
  void dispose() {
    _controller.removeListener(_reportPairingLost);
    unawaited(_controller.stop());
    _controller.dispose();
    super.dispose();
  }

  void _openLive(CameraInfo camera) => Navigator.of(context).push<void>(
    MaterialPageRoute(
      builder: (_) => LiveViewScreen(
        cameraName: camera.name,
        create: () => _controller.openLive(camera.id),
      ),
    ),
  );

  void _addDevice() => Navigator.of(context).push<void>(
    MaterialPageRoute(
      builder: (_) => AddDeviceScreen(api: widget.api, session: widget.session),
    ),
  );

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return ListenableBuilder(
      listenable: _controller,
      builder: (context, _) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          ListTile(
            title: Text(l10n.watchPairedTitle(widget.session.serverName)),
            subtitle: Text(
              '${l10n.watchPairedRole(deviceRoleText(l10n, widget.session.role))}'
              ' · ${_controller.connected ? l10n.serverOnline : l10n.serverConnecting}',
            ),
            trailing: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                IconButton(
                  tooltip: l10n.refreshButton,
                  onPressed: _controller.refreshing
                      ? null
                      : _controller.refresh,
                  icon: const Icon(Icons.refresh),
                ),
                IconButton(
                  tooltip: l10n.addDeviceButton,
                  onPressed: _addDevice,
                  icon: const Icon(Icons.add),
                ),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _controller.refresh,
              child: switch (_controller.state) {
                CameraListLoading() => const _PullableCenter(
                  child: CircularProgressIndicator(),
                ),
                CameraListFailed() => _PullableCenter(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(l10n.cameraListError, textAlign: TextAlign.center),
                      const SizedBox(height: 16),
                      FilledButton(
                        onPressed: _controller.refresh,
                        child: Text(l10n.retryButton),
                      ),
                    ],
                  ),
                ),
                CameraListLoaded(:final cameras) when cameras.isEmpty =>
                  _PullableCenter(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(l10n.cameraListEmpty, textAlign: TextAlign.center),
                        const SizedBox(height: 16),
                        FilledButton.icon(
                          onPressed: _addDevice,
                          icon: const Icon(Icons.add_a_photo_outlined),
                          label: Text(l10n.addCameraButton),
                        ),
                      ],
                    ),
                  ),
                CameraListLoaded(:final cameras) => ListView.separated(
                  physics: const AlwaysScrollableScrollPhysics(),
                  itemCount: cameras.length,
                  separatorBuilder: (_, _) => const Divider(height: 1),
                  itemBuilder: (context, index) => _CameraTile(
                    camera: cameras[index],
                    onOpen: () => _openLive(cameras[index]),
                  ),
                ),
              },
            ),
          ),
        ],
      ),
    );
  }
}

/// Centers its child and still lets the user pull to refresh.
class _PullableCenter extends StatelessWidget {
  const _PullableCenter({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) => LayoutBuilder(
    builder: (context, constraints) => SingleChildScrollView(
      physics: const AlwaysScrollableScrollPhysics(),
      child: ConstrainedBox(
        constraints: BoxConstraints(minHeight: constraints.maxHeight),
        child: Center(
          child: Padding(padding: const EdgeInsets.all(24), child: child),
        ),
      ),
    ),
  );
}

class _CameraTile extends StatelessWidget {
  const _CameraTile({required this.camera, required this.onOpen});

  final CameraInfo camera;
  final VoidCallback onOpen;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = Theme.of(context).colorScheme;
    final battery = camera.batteryLevel;
    return ListTile(
      enabled: camera.online,
      onTap: onOpen,
      leading: Icon(
        camera.online ? Icons.videocam : Icons.videocam_off_outlined,
        color: !camera.online
            ? colors.outline
            : camera.publishing
            ? colors.error
            : colors.primary,
      ),
      title: Text(camera.name),
      subtitle: Text(
        !camera.online
            ? l10n.cameraOffline
            : '${l10n.cameraOnline} · '
                  '${camera.publishing ? l10n.cameraStreaming : l10n.cameraIdle}',
      ),
      trailing: battery == null
          ? null
          : Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(
                  camera.isCharging == true
                      ? Icons.battery_charging_full
                      : Icons.battery_std,
                  size: 20,
                ),
                const SizedBox(width: 4),
                Text('$battery%'),
              ],
            ),
    );
  }
}
