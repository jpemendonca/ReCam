import 'dart:async';

import 'package:flutter/material.dart';

import '../l10n/generated/app_localizations.dart';
import 'battery_guide.dart';

/// The battery settings that keep the camera running: the ones the app reads, each checked by
/// itself or with a button that opens the right Android screen, and tips for what no app can
/// read. Never blocks: the person continues whenever they like. Checks again on coming back from
/// Android's settings.
class BatteryGuideScreen extends StatefulWidget {
  const BatteryGuideScreen({
    required this.controller,
    this.beforeCameraMode = true,
    super.key,
  });

  final BatteryGuideController controller;

  /// False when opened from the menu: the button then just closes the screen.
  final bool beforeCameraMode;

  @override
  State<BatteryGuideScreen> createState() => _BatteryGuideScreenState();
}

class _BatteryGuideScreenState extends State<BatteryGuideScreen>
    with WidgetsBindingObserver {
  BatteryStatus? _status;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    unawaited(_refresh());
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) unawaited(_refresh());
  }

  Future<void> _refresh() async {
    final status = await widget.controller.status();
    if (mounted) setState(() => _status = status);
  }

  Future<void> _fix(BatteryCheck check) async {
    await widget.controller.fix(check);
    await _refresh();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final colors = Theme.of(context).colorScheme;
    final status = _status;
    return Scaffold(
      appBar: AppBar(title: Text(l10n.batteryGuideTitle)),
      body: status == null
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(24),
              children: [
                Text(l10n.batteryGuideIntro),
                const SizedBox(height: 16),
                for (final check in BatteryCheck.values)
                  ListTile(
                    key: Key('battery-check-${check.name}'),
                    contentPadding: EdgeInsets.zero,
                    leading: status.missing.contains(check)
                        ? Icon(Icons.error_outline, color: colors.error)
                        : Icon(Icons.check_circle, color: colors.primary),
                    title: Text(switch (check) {
                      BatteryCheck.optimization =>
                        l10n.batteryCheckOptimization,
                      BatteryCheck.background => l10n.batteryCheckBackground,
                      BatteryCheck.notifications =>
                        l10n.batteryCheckNotifications,
                    }),
                    trailing: status.missing.contains(check)
                        ? TextButton(
                            onPressed: () => unawaited(_fix(check)),
                            child: Text(l10n.batteryCheckFix),
                          )
                        : null,
                  ),
                const SizedBox(height: 16),
                Text(
                  l10n.batteryTipsTitle,
                  style: Theme.of(context).textTheme.titleSmall,
                ),
                const SizedBox(height: 8),
                for (final tip in [
                  ...switch (status.guide) {
                    BatteryGuide.samsung => [l10n.batteryTipSamsung],
                    BatteryGuide.xiaomi => [l10n.batteryTipXiaomi],
                    BatteryGuide.generic => <String>[],
                  },
                  l10n.batteryTipRecents,
                ])
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('•  '),
                        Expanded(child: Text(tip)),
                      ],
                    ),
                  ),
                const SizedBox(height: 24),
                FilledButton(
                  onPressed: () => Navigator.of(context).pop(),
                  child: Text(
                    widget.beforeCameraMode
                        ? l10n.batteryGuideContinue
                        : l10n.batteryGuideDone,
                  ),
                ),
              ],
            ),
    );
  }
}
