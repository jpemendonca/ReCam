import 'dart:async';

import 'package:flutter/material.dart';

import '../core/network/api_client.dart';
import '../core/pairing/device_role.dart';
import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'add_device_screen.dart';
import 'camera_list_controller.dart';
import 'devices_view.dart';
import 'live_view_screen.dart';
import 'recording_switch.dart';
import 'recordings_timeline_screen.dart';
import 'settings_view.dart';

/// A paired Monitor: four tabs, Cameras (live and "Add camera"), Recordings, Devices and
/// Settings. It holds the one hub connection all of them use.
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
  int _tab = 0;

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
        brighten: () => _controller.openBrighten(camera.id),
        recordingSwitch: RecordingSwitch(
          list: _controller,
          cameraId: camera.id,
          color: Colors.white,
        ),
        recordingsButton: _RecordingsButton(
          onPressed: () => _openRecordings(camera),
        ),
      ),
    ),
  );

  void _openRecordings(CameraInfo camera) => Navigator.of(context).push<void>(
    MaterialPageRoute(
      builder: (_) => RecordingsTimelineScreen(
        cameraName: camera.name,
        create: () => _controller.openRecordings(camera.id),
        brighten: () => _controller.openBrighten(camera.id),
      ),
    ),
  );

  // The new camera shows up at once: the QR screen closes when it pairs, and the list reloads.
  // After the first camera, the person chooses the recording space.
  Future<void> _addCamera() async {
    final navigator = Navigator.of(context);
    final first = _controller.cameraCount == 0;
    final paired = await navigator.push<bool>(
      MaterialPageRoute(
        builder: (_) => AddDeviceScreen(
          api: widget.api,
          session: widget.session,
          role: DeviceRole.camera,
        ),
      ),
    );
    if (paired != true) return;
    await _controller.refresh();
    if (!first || !mounted) return;
    await navigator.push<void>(
      MaterialPageRoute(
        builder: (context) => Scaffold(
          appBar: AppBar(
            title: Text(AppLocalizations.of(context).firstSpaceTitle),
          ),
          body: SettingsView(
            api: widget.api,
            session: widget.session,
            recordingCameras: _controller.recordingCameras,
            onDone: () => Navigator.of(context).pop(),
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      body: switch (_tab) {
        1 => ListenableBuilder(
          listenable: _controller,
          builder: (context, _) =>
              _RecordingsTab(state: _controller.state, onOpen: _openRecordings),
        ),
        2 => DevicesView(api: widget.api, session: widget.session),
        3 => ListenableBuilder(
          listenable: _controller,
          builder: (context, _) => SettingsView(
            api: widget.api,
            session: widget.session,
            recordingCameras: _controller.recordingCameras,
          ),
        ),
        _ => _cameras(l10n),
      },
      bottomNavigationBar: NavigationBar(
        selectedIndex: _tab,
        onDestinationSelected: (index) => setState(() => _tab = index),
        destinations: [
          NavigationDestination(
            icon: const Icon(Icons.videocam_outlined),
            selectedIcon: const Icon(Icons.videocam),
            label: l10n.navCameras,
          ),
          NavigationDestination(
            icon: const Icon(Icons.video_library_outlined),
            selectedIcon: const Icon(Icons.video_library),
            label: l10n.navRecordings,
          ),
          NavigationDestination(
            icon: const Icon(Icons.devices_other_outlined),
            selectedIcon: const Icon(Icons.devices_other),
            label: l10n.navDevices,
          ),
          NavigationDestination(
            icon: const Icon(Icons.settings_outlined),
            selectedIcon: const Icon(Icons.settings),
            label: l10n.navSettings,
          ),
        ],
      ),
    );
  }

  Widget _cameras(AppLocalizations l10n) {
    return ListenableBuilder(
      listenable: _controller,
      builder: (context, _) => Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          ListTile(
            title: Text(l10n.watchPairedTitle(widget.session.serverName)),
            subtitle: Text(
              _controller.connected ? l10n.serverOnline : l10n.serverConnecting,
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
                  tooltip: l10n.addCameraButton,
                  onPressed: _addCamera,
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
                          onPressed: _addCamera,
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
                    list: _controller,
                    onRecordings: () => _openRecordings(cameras[index]),
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

/// The Recordings tab: pick a camera to open its timeline.
class _RecordingsTab extends StatelessWidget {
  const _RecordingsTab({required this.state, required this.onOpen});

  final CameraListState state;
  final void Function(CameraInfo camera) onOpen;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return switch (state) {
      CameraListLoading() => const Center(child: CircularProgressIndicator()),
      CameraListFailed() => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(l10n.cameraListError, textAlign: TextAlign.center),
        ),
      ),
      CameraListLoaded(:final cameras) when cameras.isEmpty => Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Text(l10n.recordingsNoCameras, textAlign: TextAlign.center),
        ),
      ),
      CameraListLoaded(:final cameras) => ListView(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
            child: Text(l10n.recordingsChooseCamera),
          ),
          for (final camera in cameras)
            ListTile(
              leading: const Icon(Icons.video_library_outlined),
              title: Text(camera.name),
              subtitle: camera.recording
                  ? Text(l10n.cameraModeRecording)
                  : null,
              trailing: const Icon(Icons.chevron_right),
              onTap: () => onOpen(camera),
            ),
        ],
      ),
    };
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
  const _CameraTile({
    required this.camera,
    required this.list,
    required this.onOpen,
    required this.onRecordings,
  });

  final CameraInfo camera;
  final CameraListController list;
  final VoidCallback onOpen;
  final VoidCallback onRecordings;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = Theme.of(context).colorScheme;
    final battery = camera.batteryLevel;
    final temperature = camera.temperatureC;
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
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            !camera.online
                ? l10n.cameraOffline
                : '${l10n.cameraOnline} · '
                      '${camera.publishing ? l10n.cameraStreaming : l10n.cameraIdle}',
          ),
          Row(
            children: [
              Flexible(
                child: RecordingSwitch(list: list, cameraId: camera.id),
              ),
              _RecordingsButton(onPressed: onRecordings),
            ],
          ),
        ],
      ),
      trailing: battery == null && temperature == null
          ? null
          : Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (temperature != null) ...[
                  const Icon(Icons.thermostat, size: 20),
                  Text(l10n.temperatureC(temperature.round())),
                  const SizedBox(width: 8),
                ],
                if (battery != null) ...[
                  Icon(
                    camera.isCharging == true
                        ? Icons.battery_charging_full
                        : Icons.battery_std,
                    size: 20,
                  ),
                  const SizedBox(width: 4),
                  Text('$battery%'),
                ],
              ],
            ),
    );
  }
}

class _RecordingsButton extends StatelessWidget {
  const _RecordingsButton({required this.onPressed});

  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) => IconButton(
    tooltip: AppLocalizations.of(context).recordingsOpen,
    onPressed: onPressed,
    icon: const Icon(Icons.video_library_outlined),
  );
}
