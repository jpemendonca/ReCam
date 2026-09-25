import 'package:flutter/material.dart';

import 'camera/camera_pairing_controller.dart';
import 'core/network/api_client.dart';
import 'home_shell.dart';
import 'l10n/generated/app_localizations.dart';
import 'viewer/viewer_pairing_controller.dart';

class RecamApp extends StatelessWidget {
  const RecamApp({
    required this.cameraPairing,
    required this.viewerPairing,
    required this.api,
    super.key,
  });

  final CameraPairingController cameraPairing;
  final ViewerPairingController viewerPairing;
  final ApiClient api;

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
      ),
    );
  }
}
