import 'dart:async';

import 'package:flutter/material.dart';

import 'app_reset.dart';
import 'camera/battery_guide.dart';
import 'camera/battery_guide_screen.dart';
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
import 'viewer/connect_browser_screen.dart';
import 'viewer/camera_list_controller.dart';
import 'viewer/viewer_pairing_controller.dart';
import 'viewer/watch_tab.dart';

/// The app's only screen host. A phone is a camera or a Monitor, never both, so it shows the
/// screen of its role, or the first-run screen while it has none.
class HomeShell extends StatefulWidget {
  const HomeShell({
    required this.cameraPairing,
    required this.viewerPairing,
    required this.api,
    required this.cameraMode,
    required this.batteryGuide,
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
  final BatteryGuideController batteryGuide;
  final CameraListFactory cameraList;
  final LinkSource links;
  final PairingCodeReader readCode;

  /// Completes when both roles have loaded their saved pairing.
  final Future<void> ready;

  @override
  State<HomeShell> createState() => _HomeShellState();
}

enum _Role { loading, none, camera, monitor }

class _HomeShellState extends State<HomeShell> {
  StreamSubscription<Uri>? _links;

  /// A code for the other role, read before this phone left its role; the first-run screen
  /// picks it up.
  String? _pendingCode;
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
    unawaited(widget.ready.then((_) => _reset.keepOneRole()));
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

  /// Reads a code while this phone already has a role, to switch it.
  Future<void> _scan() async {
    final code = await widget.readCode(context);
    if (code == null) return;
    await _route(code);
  }

  /// Pairs the role the code is for. A code for the other role first asks to leave the current
  /// one; a code for the role this phone already has changes nothing.
  Future<void> _route(String code, {String? cameraName}) async {
    final target = _router.targetOf(code);
    if (target == null || !mounted) return;
    if (_router.conflictWith(target) != null) {
      if (!await _confirmSwitch(target) || !mounted) return;
      await _resetWithProgress();
      if (mounted) setState(() => _pendingCode = code);
      return;
    }
    final l10n = AppLocalizations.of(context);
    setState(() => _pendingCode = null);
    await _router.pair(
      target,
      code,
      cameraName: cameraName ?? l10n.defaultCameraName,
      viewerName: l10n.viewerDeviceName,
    );
  }

  Future<bool> _confirmSwitch(PairingLinkTarget target) async {
    final l10n = AppLocalizations.of(context);
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(switch (target) {
          PairingLinkTarget.viewer => l10n.switchToMonitorTitle,
          PairingLinkTarget.camera => l10n.switchToCameraTitle,
        }),
        content: Text(l10n.switchRoleMessage),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: Text(l10n.cancelButton),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(l10n.switchRoleConfirm),
          ),
        ],
      ),
    );
    return confirmed == true;
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

  void _connectBrowser(ViewerPaired paired) => Navigator.of(context).push<void>(
    MaterialPageRoute(
      builder: (_) => ConnectBrowserScreen(
        api: widget.api,
        session: paired.session,
        readCode: widget.readCode,
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
    await _resetWithProgress();
  }

  Future<void> _openBatteryGuide() => Navigator.of(context).push<void>(
    MaterialPageRoute(
      builder: (_) => BatteryGuideScreen(
        controller: widget.batteryGuide,
        beforeCameraMode: false,
      ),
    ),
  );

  Future<void> _resetWithProgress() async {
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
  }

  _Role get _role {
    final camera = widget.cameraPairing.state;
    final viewer = widget.viewerPairing.state;
    if (camera is CameraPairingLoading || viewer is ViewerPairingLoading) {
      return _Role.loading;
    }
    if (camera is! CameraNotPaired) return _Role.camera;
    if (viewer is! ViewerNotPaired) return _Role.monitor;
    return _Role.none;
  }

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: Listenable.merge([widget.cameraPairing, widget.viewerPairing]),
    builder: (context, _) => _buildShell(context),
  );

  Widget _buildShell(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final role = _role;
    final cameraState = widget.cameraPairing.state;
    final viewerState = widget.viewerPairing.state;
    return Scaffold(
      appBar: AppBar(
        title: Text(switch (role) {
          _Role.camera => l10n.appTitleCamera,
          _Role.monitor => l10n.appTitleMonitor,
          _Role.loading || _Role.none => l10n.appTitle,
        }),
        actions: [
          if (role == _Role.camera || role == _Role.monitor)
            PopupMenuButton<_MenuAction>(
              onSelected: (action) => switch ((action, viewerState)) {
                (_MenuAction.addMonitor, final ViewerPaired paired) =>
                  _addMonitor(paired),
                (_MenuAction.connectBrowser, final ViewerPaired paired) =>
                  _connectBrowser(paired),
                (_MenuAction.scan, _) => unawaited(_scan()),
                (_MenuAction.reset, _) => unawaited(_confirmReset()),
                (_MenuAction.battery, _) => unawaited(_openBatteryGuide()),
                _ => null,
              },
              itemBuilder: (context) => [
                if (viewerState is ViewerPaired) ...[
                  PopupMenuItem(
                    value: _MenuAction.addMonitor,
                    child: Text(l10n.addMonitorButton),
                  ),
                  PopupMenuItem(
                    value: _MenuAction.connectBrowser,
                    child: Text(l10n.connectBrowserButton),
                  ),
                ],
                if (role == _Role.camera)
                  PopupMenuItem(
                    value: _MenuAction.battery,
                    child: Text(l10n.batteryGuideMenu),
                  ),
                PopupMenuItem(
                  value: _MenuAction.scan,
                  child: Text(l10n.scanQrButton),
                ),
                PopupMenuItem(
                  value: _MenuAction.reset,
                  child: Text(l10n.resetAppButton),
                ),
              ],
            ),
        ],
      ),
      // Both role screens stay built under the first-run screen, so the camera screen sees
      // its pairing finish and opens camera mode.
      body: SafeArea(
        child: Stack(
          children: [
            Offstage(
              offstage: role == _Role.none,
              child: IndexedStack(
                index: role == _Role.monitor ? 1 : 0,
                children: [
                  CameraTab(
                    pairing: widget.cameraPairing,
                    cameraMode: widget.cameraMode,
                    batteryGuide: widget.batteryGuide,
                  ),
                  WatchTab(
                    pairing: widget.viewerPairing,
                    api: widget.api,
                    cameraList: widget.cameraList,
                  ),
                ],
              ),
            ),
            if (role == _Role.none)
              FirstRunScreen(
                key: ValueKey(_pendingCode),
                initialCode: _pendingCode,
                readCode: () => widget.readCode(context),
                onPair: (code, cameraName) =>
                    _route(code, cameraName: cameraName),
                failure: switch ((cameraState, viewerState)) {
                  (CameraNotPaired(:final lastFailure?), _) => lastFailure,
                  (_, ViewerNotPaired(:final lastFailure?)) => lastFailure,
                  _ => null,
                },
              ),
          ],
        ),
      ),
    );
  }
}

enum _MenuAction { addMonitor, connectBrowser, scan, reset, battery }
