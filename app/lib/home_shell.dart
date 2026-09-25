import 'package:flutter/material.dart';

import 'camera/camera_tab.dart';
import 'core/network/api_client.dart';
import 'l10n/generated/app_localizations.dart';
import 'viewer/viewer_pairing_controller.dart';
import 'viewer/watch_tab.dart';

class HomeShell extends StatefulWidget {
  const HomeShell({required this.viewerPairing, required this.api, super.key});

  final ViewerPairingController viewerPairing;
  final ApiClient api;

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  int _selectedIndex = 0;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      body: SafeArea(
        child: IndexedStack(
          index: _selectedIndex,
          children: [
            const CameraTab(),
            WatchTab(pairing: widget.viewerPairing, api: widget.api),
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
