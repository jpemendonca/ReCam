import 'package:flutter/material.dart';

import '../l10n/generated/app_localizations.dart';
import 'camera_list_controller.dart';

/// "Record always" for one camera, kept in sync with the camera list. Off, with the reason, for
/// a camera that cannot record.
class RecordingSwitch extends StatelessWidget {
  const RecordingSwitch({
    required this.list,
    required this.cameraId,
    this.color,
    super.key,
  });

  final CameraListController list;
  final String cameraId;

  /// Label color, for dark backgrounds like the live view.
  final Color? color;

  Future<void> _change(BuildContext context, bool enabled) async {
    final messenger = ScaffoldMessenger.of(context);
    final failed = AppLocalizations.of(context).recordingFailed;
    if (!await list.setRecording(cameraId, enabled)) {
      messenger.showSnackBar(SnackBar(content: Text(failed)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return ListenableBuilder(
      listenable: list,
      builder: (context, _) {
        final camera = list.camera(cameraId);
        if (camera == null) return const SizedBox.shrink();
        final label = camera.canRecord
            ? l10n.recordAlways
            : l10n.recordingNeedsH264;
        return Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Flexible(
              child: Text(
                label,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(color: color),
              ),
            ),
            Switch(
              value: camera.recording,
              onChanged: camera.canRecord
                  ? (enabled) => _change(context, enabled)
                  : null,
            ),
          ],
        );
      },
    );
  }
}
