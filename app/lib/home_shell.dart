import 'dart:async';

import 'package:flutter/material.dart';

import 'camera/camera_mode_controller.dart';
import 'camera/camera_pairing_controller.dart';
import 'camera/camera_tab.dart';
import 'core/network/api_client.dart';
import 'core/pairing/pairing_link.dart';
import 'l10n/generated/app_localizations.dart';
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

  /// Opens the tab the link is for and pairs it, unless that tab is already paired.
  Future<void> _onLink(Uri uri) async {
    final link = uri.toString();
    final target = pairingLinkTarget(link);
    if (target == null) return;
    await widget.ready;
    if (!mounted) return;
    final l10n = AppLocalizations.of(context);
    switch (target) {
      case PairingLinkTarget.camera:
        setState(() => _selectedIndex = _cameraTab);
        if (widget.cameraPairing.state is CameraNotPaired) {
          await widget.cameraPairing.submitQr(
            link,
            name: l10n.defaultCameraName,
          );
        }
      case PairingLinkTarget.viewer:
        setState(() => _selectedIndex = _watchTab);
        if (widget.viewerPairing.state is ViewerNotPaired) {
          await widget.viewerPairing.submitQr(
            link,
            deviceName: l10n.ownerDeviceName,
          );
        }
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      body: SafeArea(
        child: IndexedStack(
          index: _selectedIndex,
          children: [
            CameraTab(
              pairing: widget.cameraPairing,
              cameraMode: widget.cameraMode,
            ),
            WatchTab(
              pairing: widget.viewerPairing,
              api: widget.api,
              cameraList: widget.cameraList,
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
