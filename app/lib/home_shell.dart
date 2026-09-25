import 'dart:async';

import 'package:flutter/material.dart';

import 'app_reset.dart';
import 'camera/camera_mode_controller.dart';
import 'camera/camera_pairing_controller.dart';
import 'camera/camera_tab.dart';
import 'core/network/api_client.dart';
import 'core/pairing/pairing_link.dart';
import 'core/scanner/qr_scanner_screen.dart';
import 'l10n/generated/app_localizations.dart';
import 'pairing_router.dart';
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
    required this.ready,
    super.key,
  });

  final CameraPairingController cameraPairing;
  final ViewerPairingController viewerPairing;
  final ApiClient api;
  final CameraModeFactory cameraMode;
  final CameraListFactory cameraList;
  final LinkSource links;

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
    final code = await Navigator.of(
      context,
    ).push<String>(MaterialPageRoute(builder: (_) => const QrScannerScreen()));
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
      viewerName: l10n.ownerDeviceName,
    );
  }

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
    if (confirmed != true) return;
    await _reset.reset();
    if (mounted) setState(() => _selectedIndex = _cameraTab);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.appTitle),
        actions: [
          PopupMenuButton<_MenuAction>(
            onSelected: (action) => switch (action) {
              _MenuAction.reset => unawaited(_confirmReset()),
            },
            itemBuilder: (context) => [
              PopupMenuItem(
                value: _MenuAction.reset,
                child: Text(l10n.resetAppButton),
              ),
            ],
          ),
        ],
      ),
      body: SafeArea(
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
      bottomNavigationBar: NavigationBar(
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

enum _MenuAction { reset }
