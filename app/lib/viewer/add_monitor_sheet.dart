import 'package:flutter/material.dart';

import '../l10n/generated/app_localizations.dart';

/// Where the new Monitor will watch.
enum AddMonitorTarget { phone, browser }

/// "Add Monitor": asks where the new Monitor will watch. Another phone scans the QR code this
/// phone shows; a browser shows its own QR code, and this phone scans it.
class AddMonitorSheet extends StatelessWidget {
  const AddMonitorSheet({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return SafeArea(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(24, 24, 24, 8),
            child: Text(
              l10n.addMonitorWhere,
              style: Theme.of(context).textTheme.titleLarge,
            ),
          ),
          ListTile(
            leading: const Icon(Icons.phone_android),
            title: Text(l10n.addMonitorOnPhone),
            subtitle: Text(l10n.addMonitorOnPhoneHint),
            onTap: () => Navigator.of(context).pop(AddMonitorTarget.phone),
          ),
          ListTile(
            leading: const Icon(Icons.computer),
            title: Text(l10n.addMonitorInBrowser),
            subtitle: Text(l10n.addMonitorInBrowserHint),
            onTap: () => Navigator.of(context).pop(AddMonitorTarget.browser),
          ),
          const SizedBox(height: 16),
        ],
      ),
    );
  }
}
