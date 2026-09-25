import 'dart:async';

import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../core/network/api_client.dart';
import '../core/storage/credential_store.dart';
import '../l10n/generated/app_localizations.dart';
import 'recordings_controller.dart';

/// "Recordings": the slider for the space all recordings may take on the server.
class RecordingsScreen extends StatefulWidget {
  const RecordingsScreen({required this.api, required this.session, super.key});

  final ApiClient api;
  final PairedSession session;

  @override
  State<RecordingsScreen> createState() => _RecordingsScreenState();
}

class _RecordingsScreenState extends State<RecordingsScreen> {
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
      '0',
      locale,
    ).format(RecordingsController.hoursFor(megabytes));
    return Scaffold(
      appBar: AppBar(title: Text(l10n.recordingsTitle)),
      body: ListenableBuilder(
        listenable: _controller,
        builder: (context, _) => switch (_controller.state) {
          RecordingsLoading() => const Center(
            child: CircularProgressIndicator(),
          ),
          RecordingsFailed() => Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(l10n.recordingsLoadFailed, textAlign: TextAlign.center),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: _controller.load,
                  child: Text(l10n.retryButton),
                ),
              ],
            ),
          ),
          RecordingsReady(:final quota, :final selectedMegabytes) => ListView(
            padding: const EdgeInsets.all(24),
            children: [
              Text(
                l10n.recordingsInUse(
                  gigabytes(quota.usedBytes / RecordingQuota.bytesPerMegabyte),
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
              Text(l10n.recordingsHours(hours(selectedMegabytes))),
              const SizedBox(height: 8),
              Text(
                l10n.recordingsHowItWorks,
                style: Theme.of(context).textTheme.bodySmall,
              ),
              const SizedBox(height: 24),
              FilledButton(
                onPressed:
                    _controller.saving || selectedMegabytes == quota.megabytes
                    ? null
                    : _save,
                child: Text(l10n.recordingsSave),
              ),
            ],
          ),
        },
      ),
    );
  }
}
