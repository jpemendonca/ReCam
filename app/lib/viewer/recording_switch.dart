import 'package:flutter/material.dart';

import '../core/network/api_client.dart';
import '../l10n/generated/app_localizations.dart';
import 'camera_list_controller.dart';

/// "Record" for one camera, kept in sync with the camera list, with what the server really sees
/// next to it: recording, starting, or not recording and why. Off, with the reason, for a camera
/// that cannot record.
class RecordingSwitch extends StatelessWidget {
  const RecordingSwitch({
    required this.list,
    required this.cameraId,
    this.color,
    super.key,
  });

  final CameraListController list;
  final String cameraId;

  /// Text color, for dark backgrounds like the live view.
  final Color? color;

  static const _starting = Color(0xFFF9A825);

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
    final danger = Theme.of(context).colorScheme.error;
    return ListenableBuilder(
      listenable: list,
      builder: (context, _) {
        final camera = list.camera(cameraId);
        if (camera == null) return const SizedBox.shrink();
        final state = camera.shownRecordingState;
        final (text, dot) = switch (state) {
          RecordingState.recording => (l10n.recordingStateRecording, danger),
          RecordingState.starting => (l10n.recordingStateStarting, _starting),
          RecordingState.offline => (l10n.recordingStateOffline, null),
          RecordingState.noSpace => (l10n.recordingStateNoSpace, null),
          RecordingState.stalled => (l10n.recordingStateStalled, null),
          RecordingState.needsH264 => (l10n.recordingNeedsH264, null),
          RecordingState.off => (l10n.recordingStateOff, null),
        };
        final problem = dot == null && state != RecordingState.off;
        return Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              dot != null ? Icons.circle : Icons.error_outline,
              key: const Key('recording-state-dot'),
              size: 12,
              color: dot ?? (problem ? danger : color),
            ),
            const SizedBox(width: 6),
            Flexible(
              child: Text(
                text,
                key: const Key('recording-state'),
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                  color: problem || state == RecordingState.recording
                      ? danger
                      : color,
                  fontWeight: state == RecordingState.recording
                      ? FontWeight.w600
                      : null,
                ),
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
