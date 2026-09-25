import 'package:flutter/material.dart';

import '../l10n/generated/app_localizations.dart';
import 'battery_guide.dart';

/// Shown before camera mode while Android still restricts the app's battery use. Pops when the
/// person continues, freed or not.
class BatteryGuideScreen extends StatelessWidget {
  const BatteryGuideScreen({
    required this.guide,
    required this.controller,
    super.key,
  });

  final BatteryGuide guide;
  final BatteryGuideController controller;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final steps = switch (guide) {
      BatteryGuide.samsung => [
        l10n.batteryGuideSamsung1,
        l10n.batteryGuideSamsung2,
      ],
      BatteryGuide.xiaomi => [
        l10n.batteryGuideXiaomi1,
        l10n.batteryGuideXiaomi2,
      ],
      BatteryGuide.generic => [l10n.batteryGuideGeneric1],
    };
    return Scaffold(
      appBar: AppBar(title: Text(l10n.batteryGuideTitle)),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(l10n.batteryGuideIntro),
            const SizedBox(height: 16),
            for (final (index, step) in steps.indexed)
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: CircleAvatar(child: Text('${index + 1}')),
                title: Text(step),
              ),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: controller.optimization.requestIgnore,
              icon: const Icon(Icons.battery_saver_outlined),
              label: Text(l10n.batteryGuideAllow),
            ),
            if (guide != BatteryGuide.generic) ...[
              const SizedBox(height: 8),
              OutlinedButton.icon(
                onPressed: controller.optimization.openAppSettings,
                icon: const Icon(Icons.settings_outlined),
                label: Text(l10n.batteryGuideOpenSettings),
              ),
            ],
            const SizedBox(height: 24),
            TextButton(
              onPressed: () => Navigator.of(context).pop(),
              child: Text(l10n.batteryGuideContinue),
            ),
          ],
        ),
      ),
    );
  }
}
