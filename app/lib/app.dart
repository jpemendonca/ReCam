import 'package:flutter/material.dart';

import 'camera/battery_guide.dart';
import 'camera/camera_mode_controller.dart';
import 'camera/camera_pairing_controller.dart';
import 'core/network/api_client.dart';
import 'core/pairing/pairing_link.dart';
import 'core/scanner/qr_scanner_screen.dart';
import 'home_shell.dart';
import 'l10n/generated/app_localizations.dart';
import 'viewer/camera_list_controller.dart';
import 'viewer/viewer_pairing_controller.dart';

class RecamApp extends StatelessWidget {
  const RecamApp({
    required this.cameraPairing,
    required this.viewerPairing,
    required this.api,
    required this.cameraMode,
    required this.batteryGuide,
    required this.cameraList,
    required this.links,
    required this.ready,
    this.readCode = readPairingCode,
    super.key,
  });

  final CameraPairingController cameraPairing;
  final ViewerPairingController viewerPairing;
  final ApiClient api;
  final CameraModeFactory cameraMode;
  final BatteryGuideController batteryGuide;
  final CameraListFactory cameraList;
  final LinkSource links;
  final Future<void> ready;
  final PairingCodeReader readCode;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      onGenerateTitle: (context) => AppLocalizations.of(context).appTitle,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      supportedLocales: AppLocalizations.supportedLocales,
      theme: ThemeData(colorSchemeSeed: Colors.teal, useMaterial3: true),
      darkTheme: ThemeData(
        colorSchemeSeed: Colors.teal,
        brightness: Brightness.dark,
        useMaterial3: true,
      ),
      home: HomeShell(
        cameraPairing: cameraPairing,
        viewerPairing: viewerPairing,
        api: api,
        cameraMode: cameraMode,
        batteryGuide: batteryGuide,
        cameraList: cameraList,
        links: links,
        readCode: readCode,
        ready: ready,
      ),
    );
  }
}
