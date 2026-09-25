import 'dart:async';

import 'package:flutter/material.dart';

import '../core/network/api_client.dart';
import '../core/pairing/device_role.dart';
import '../core/pairing/pairing_labels.dart';
import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'add_camera_screen.dart';
import 'camera_list_controller.dart';
import 'live_view_screen.dart';

/// The Watch tab once paired: the cameras, live, and "Add camera" for the owner.
class CameraListView extends StatefulWidget {
  const CameraListView({
    required this.api,
    required this.session,
    required this.cameraList,
    super.key,
  });

  final ApiClient api;
  final PairedSession session;
  final CameraListFactory cameraList;

  @override
  State<CameraListView> createState() => _CameraListViewState();
}

class _CameraListViewState extends State<CameraListView> {
  late final CameraListController _controller;

  @override
  void initState() {
    super.initState();
    _controller = widget.cameraList(widget.session);
    unawaited(_controller.start());
  }

  @override
  void dispose() {
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

  void _addCamera() => Navigator.of(context).push<void>(
    MaterialPageRoute(
      builder: (_) => AddCameraScreen(api: widget.api, session: widget.session),
    ),
  );

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final isOwner = widget.session.role == DeviceRole.owner;
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
            trailing: isOwner
                ? IconButton(
                    tooltip: l10n.addCameraButton,
                    onPressed: _addCamera,
                    icon: const Icon(Icons.add_a_photo_outlined),
                  )
                : null,
          ),
          const Divider(height: 1),
          Expanded(
            child: switch (_controller.state) {
              CameraListLoading() => const Center(
                child: CircularProgressIndicator(),
              ),
              CameraListFailed() => Center(
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
              CameraListLoaded(:final cameras) when cameras.isEmpty => Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(l10n.cameraListEmpty, textAlign: TextAlign.center),
                      if (isOwner) ...[
                        const SizedBox(height: 16),
                        FilledButton.icon(
                          onPressed: _addCamera,
                          icon: const Icon(Icons.add_a_photo_outlined),
                          label: Text(l10n.addCameraButton),
                        ),
                      ],
                    ],
                  ),
                ),
              ),
              CameraListLoaded(:final cameras) => ListView.separated(
                itemCount: cameras.length,
                separatorBuilder: (_, _) => const Divider(height: 1),
                itemBuilder: (context, index) => _CameraTile(
                  camera: cameras[index],
                  onOpen: () => _openLive(cameras[index]),
                ),
              ),
            },
          ),
        ],
      ),
    );
  }
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
        color: camera.online ? colors.primary : colors.outline,
      ),
      title: Text(camera.name),
      subtitle: Text(camera.online ? l10n.cameraOnline : l10n.cameraOffline),
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
