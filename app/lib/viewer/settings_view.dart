import 'dart:async';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../core/language/language_picker.dart';
import '../core/network/api_client.dart';
import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'recordings_controller.dart';

/// The Monitor's Settings tab: for now, the slider for the space all recordings may take on the
/// server.
class SettingsView extends StatefulWidget {
  const SettingsView({
    required this.api,
    required this.session,
    required this.recordingCameras,
    this.onDone,
    super.key,
  });

  final ApiClient api;
  final PairedSession session;

  /// How many cameras record now; the hours that fit are shared among them.
  final int recordingCameras;

  /// Set right after the first camera pairs: the view asks for the space once, and runs this
  /// when the person saves or skips.
  final VoidCallback? onDone;

  @override
  State<SettingsView> createState() => _SettingsViewState();
}

class _SettingsViewState extends State<SettingsView> {
  late final _controller = RecordingsController(
    api: widget.api,
    session: widget.session,
  );

  @override
  void initState() {
    super.initState();
    unawaited(_controller.load());
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    final messenger = ScaffoldMessenger.of(context);
    final l10n = AppLocalizations.of(context);
    final saved = await _controller.save();
    final onDone = widget.onDone;
    if (saved && onDone != null) {
      onDone();
      return;
    }
    messenger.showSnackBar(
      SnackBar(
        content: Text(saved ? l10n.recordingsSaved : l10n.recordingsSaveFailed),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final locale = Localizations.localeOf(context).toString();
    String gigabytes(num megabytes) =>
        NumberFormat('0.#', locale).format(megabytes / 1024);
    String hours(int megabytes) => NumberFormat(
      '0.#',
      locale,
    ).format(RecordingsController.hoursFor(megabytes, widget.recordingCameras));
    String hoursText(int megabytes) => widget.recordingCameras == 0
        ? l10n.recordingsHours(hours(megabytes))
        : l10n.recordingsHoursCameras(
            widget.recordingCameras,
            hours(megabytes),
          );
    return ListenableBuilder(
      listenable: _controller,
      builder: (context, _) => ListView(
        padding: const EdgeInsets.all(24),
        children: [
          switch (_controller.state) {
            RecordingsLoading() => const Padding(
              padding: EdgeInsets.all(24),
              child: Center(child: CircularProgressIndicator()),
            ),
            RecordingsFailed() => Column(
              children: [
                Text(l10n.recordingsLoadFailed, textAlign: TextAlign.center),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: _controller.load,
                  child: Text(l10n.retryButton),
                ),
              ],
            ),
            RecordingsReady(:final quota, :final selectedMegabytes) => Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  l10n.recordingsSpaceTitle,
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                if (widget.onDone != null) ...[
                  const SizedBox(height: 8),
                  Text(l10n.firstSpaceIntro),
                ],
                const SizedBox(height: 8),
                Text(l10n.settingsSpaceScope),
                const SizedBox(height: 16),
                Text(
                  l10n.recordingsInUse(
                    gigabytes(
                      quota.usedBytes / RecordingQuota.bytesPerMegabyte,
                    ),
                    gigabytes(quota.megabytes),
                  ),
                ),
                const SizedBox(height: 24),
                Text(
                  l10n.recordingsSpace(gigabytes(selectedMegabytes)),
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                Slider(
                  value: selectedMegabytes.toDouble(),
                  min: RecordingsController.minimumMegabytes.toDouble(),
                  max: quota.maxMegabytes
                      .clamp(RecordingsController.minimumMegabytes + 1, 1 << 30)
                      .toDouble(),
                  onChanged: (value) => _controller.select(value.round()),
                ),
                Text(hoursText(selectedMegabytes)),
                const SizedBox(height: 8),
                Text(
                  l10n.recordingsHowItWorks,
                  style: Theme.of(context).textTheme.bodySmall,
                ),
                const SizedBox(height: 24),
                if (widget.onDone case final onDone?) ...[
                  FilledButton(
                    onPressed: _controller.saving ? null : _save,
                    child: Text(l10n.firstSpaceSave),
                  ),
                  const SizedBox(height: 8),
                  TextButton(
                    onPressed: onDone,
                    child: Text(l10n.firstSpaceSkip),
                  ),
                ] else
                  FilledButton(
                    onPressed:
                        _controller.saving ||
                            selectedMegabytes == quota.megabytes
                        ? null
                        : _save,
                    child: Text(l10n.recordingsSave),
                  ),
              ],
            ),
          },
          // The language is this phone's choice; it has no place in the first-space screen.
          if (widget.onDone == null) ...[
            const SizedBox(height: 32),
            const LanguagePicker(),
          ],
        ],
      ),
    );
  }
}
