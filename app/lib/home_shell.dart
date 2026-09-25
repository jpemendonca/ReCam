import 'dart:async';

import 'package:flutter/material.dart';

import 'app_reset.dart';
import 'camera/camera_mode_controller.dart';
import 'camera/camera_pairing_controller.dart';
import 'camera/camera_tab.dart';
import 'core/network/api_client.dart';
import 'core/pairing/pairing_link.dart';
import 'core/scanner/qr_scanner_screen.dart';
import 'first_run_screen.dart';
import 'l10n/generated/app_localizations.dart';
import 'core/pairing/device_role.dart';
import 'pairing_router.dart';
import 'viewer/add_device_screen.dart';
import 'viewer/camera_list_controller.dart';
import 'viewer/viewer_pairing_controller.dart';
import 'viewer/watch_tab.dart';

class HomeShell extends StatefulWidget {
  const HomeShell({
    required this.cameraPairing,
    required this.viewerPairing,
    required this.api,
    required this.cameraMode,
    required this.cameraList,
    required this.links,
    required this.readCode,
    required this.ready,
    super.key,
  });

  final CameraPairingController cameraPairing;
  final ViewerPairingController viewerPairing;
  final ApiClient api;
  final CameraModeFactory cameraMode;
  final CameraListFactory cameraList;
  final LinkSource links;
  final PairingCodeReader readCode;

  /// Completes when both tabs have loaded their saved pairing.
  final Future<void> ready;

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  static const _cameraTab = 0;
  static const _watchTab = 1;

  int _selectedIndex = _cameraTab;
  StreamSubscription<Uri>? _links;
  late final _reset = AppReset(
    camera: widget.cameraPairing,
    viewer: widget.viewerPairing,
    api: widget.api,
  );
  late final _router = PairingRouter(
    camera: widget.cameraPairing,
    viewer: widget.viewerPairing,
  );

  @override
  void initState() {
    super.initState();
    _links = widget.links.links.listen(_onLink);
  }

  @override
  void dispose() {
    _links?.cancel();
    super.dispose();
  }

  Future<void> _onLink(Uri uri) async {
    await widget.ready;
    await _route(uri.toString());
  }

  /// The one QR reader of the app. The code decides which tab pairs, not the tab that asked.
  Future<void> _scan({
    required PairingLinkTarget from,
    String? cameraName,
  }) async {
    final code = await widget.readCode(context);
    if (code == null) return;
    await _route(code, from: from, cameraName: cameraName);
  }

  /// Opens the tab the code is for and pairs it, unless that tab is already paired.
  Future<void> _route(
    String code, {
    PairingLinkTarget? from,
    String? cameraName,
  }) async {
    final target = _router.targetOf(code, from: from);
    if (target == null || !mounted) return;
    final l10n = AppLocalizations.of(context);
    setState(
      () => _selectedIndex = switch (target) {
        PairingLinkTarget.camera => _cameraTab,
        PairingLinkTarget.viewer => _watchTab,
      },
    );
    await _router.pair(
      target,
      code,
      cameraName: cameraName ?? l10n.defaultCameraName,
      viewerName: l10n.viewerDeviceName,
    );
  }

  void _addMonitor(ViewerPaired paired) => Navigator.of(context).push<void>(
    MaterialPageRoute(
      builder: (_) => AddDeviceScreen(
        api: widget.api,
        session: paired.session,
        role: DeviceRole.viewer,
      ),
    ),
  );

  Future<void> _confirmReset() async {
    final l10n = AppLocalizations.of(context);
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.resetAppTitle),
        content: Text(l10n.resetAppMessage),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: Text(l10n.cancelButton),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(l10n.resetAppConfirm),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    final navigator = Navigator.of(context);
    // Telling the server can take a few seconds when it does not answer.
    unawaited(
      showDialog<void>(
        context: context,
        barrierDismissible: false,
        builder: (_) => const Center(child: CircularProgressIndicator()),
      ),
    );
    await _reset.reset();
    navigator.pop();
    if (mounted) setState(() => _selectedIndex = _cameraTab);
  }

  bool get _nothingPaired =>
      widget.cameraPairing.state is CameraNotPaired &&
      widget.viewerPairing.state is ViewerNotPaired;

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: Listenable.merge([widget.cameraPairing, widget.viewerPairing]),
    builder: (context, _) => _buildShell(context),
  );

  Widget _buildShell(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final firstRun = _nothingPaired;
    final cameraState = widget.cameraPairing.state;
    final viewerState = widget.viewerPairing.state;
    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.appTitle),
        actions: [
          if (!firstRun)
            PopupMenuButton<_MenuAction>(
              onSelected: (action) => switch ((action, viewerState)) {
                (_MenuAction.addMonitor, final ViewerPaired paired) =>
                  _addMonitor(paired),
                (_MenuAction.addMonitor, _) => null,
                (_MenuAction.reset, _) => unawaited(_confirmReset()),
              },
              itemBuilder: (context) => [
                if (viewerState is ViewerPaired)
                  PopupMenuItem(
                    value: _MenuAction.addMonitor,
                    child: Text(l10n.addMonitorButton),
                  ),
                PopupMenuItem(
                  value: _MenuAction.reset,
                  child: Text(l10n.resetAppButton),
                ),
              ],
            ),
        ],
      ),
      // The tabs stay built under the first-run screen, so the Camera tab sees its pairing
      // finish and opens camera mode.
      body: SafeArea(
        child: Stack(
          children: [
            Offstage(
              offstage: firstRun,
              child: IndexedStack(
                index: _selectedIndex,
                children: [
                  CameraTab(
                    pairing: widget.cameraPairing,
                    cameraMode: widget.cameraMode,
                    onScan: (name) =>
                        _scan(from: PairingLinkTarget.camera, cameraName: name),
                  ),
                  WatchTab(
                    pairing: widget.viewerPairing,
                    api: widget.api,
                    cameraList: widget.cameraList,
                    onScan: () => _scan(from: PairingLinkTarget.viewer),
                  ),
                ],
              ),
            ),
            if (firstRun)
              FirstRunScreen(
                onCamera: (name) =>
                    _scan(from: PairingLinkTarget.camera, cameraName: name),
                onWatch: () => _scan(from: PairingLinkTarget.viewer),
                failure: switch ((cameraState, viewerState)) {
                  (CameraNotPaired(:final lastFailure?), _) => lastFailure,
                  (_, ViewerNotPaired(:final lastFailure?)) => lastFailure,
                  _ => null,
                },
              ),
          ],
        ),
      ),
      bottomNavigationBar: firstRun
          ? null
          : NavigationBar(
              selectedIndex: _selectedIndex,
              onDestinationSelected: (index) =>
                  setState(() => _selectedIndex = index),
              destinations: [
                NavigationDestination(
                  icon: const Icon(Icons.videocam_outlined),
                  selectedIcon: const Icon(Icons.videocam),
                  label: l10n.cameraTab,
                ),
                NavigationDestination(
                  icon: const Icon(Icons.live_tv_outlined),
                  selectedIcon: const Icon(Icons.live_tv),
                  label: l10n.watchTab,
                ),
              ],
            ),
    );
  }
}

enum _MenuAction { addMonitor, reset }
