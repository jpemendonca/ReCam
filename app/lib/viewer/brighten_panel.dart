import 'package:flutter/material.dart';

import '../core/media/image_adjustment.dart';
import '../l10n/generated/app_localizations.dart';
import 'brighten_controller.dart';

/// The "Brighten" sliders, over the bottom of the video.
class BrightenPanel extends StatelessWidget {
  const BrightenPanel({required this.controller, super.key});

  final BrightenController controller;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final adjustment = controller.adjustment;
    return Material(
      color: Colors.black87,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            _Row(
              label: l10n.brightenBrightness,
              child: Slider(
                key: const Key('brighten-brightness'),
                value: adjustment.brightness,
                min: ImageAdjustment.minBrightness,
                max: ImageAdjustment.maxBrightness,
                divisions: 20,
                onChanged: controller.setBrightness,
              ),
            ),
            _Row(
              label: l10n.brightenContrast,
              child: Slider(
                key: const Key('brighten-contrast'),
                value: adjustment.contrast,
                min: ImageAdjustment.minContrast,
                max: ImageAdjustment.maxContrast,
                divisions: 15,
                onChanged: controller.setContrast,
              ),
            ),
            Align(
              alignment: Alignment.centerRight,
              child: TextButton(
                onPressed: adjustment.isNormal ? null : controller.reset,
                child: Text(l10n.brightenReset),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({required this.label, required this.child});

  final String label;
  final Widget child;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      SizedBox(
        width: 96,
        child: Text(label, style: const TextStyle(color: Colors.white)),
      ),
      Expanded(child: child),
    ],
  );
}

/// The video with this camera's "Brighten" setting applied, on this screen only.
class BrightenedVideo extends StatelessWidget {
  const BrightenedVideo({
    required this.controller,
    required this.child,
    super.key,
  });

  final BrightenController controller;
  final Widget child;

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: controller,
    builder: (context, _) => controller.adjustment.isNormal
        ? child
        : ColorFiltered(
            colorFilter: controller.adjustment.filter,
            child: child,
          ),
  );
}

/// Opens and closes the "Brighten" sliders.
class BrightenButton extends StatelessWidget {
  const BrightenButton({required this.controller, super.key});

  final BrightenController controller;

  @override
  Widget build(BuildContext context) => ListenableBuilder(
    listenable: controller,
    builder: (context, _) => IconButton(
      tooltip: AppLocalizations.of(context).brightenButton,
      isSelected: controller.open,
      onPressed: controller.toggle,
      icon: const Icon(Icons.brightness_6_outlined),
      selectedIcon: const Icon(Icons.brightness_6),
    ),
  );
}
