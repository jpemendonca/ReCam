import 'package:flutter/material.dart';

import 'core/pairing/device_name_validator.dart';
import 'core/pairing/pairing_labels.dart';
import 'core/pairing/pairing_service.dart';
import 'l10n/generated/app_localizations.dart';

/// What a phone with nothing paired shows: the person says what this phone is for (Watch first,
/// the usual first step, then Film), and the QR reader opens. The code read still decides which tab pairs.
class FirstRunScreen extends StatefulWidget {
  const FirstRunScreen({
    required this.onCamera,
    required this.onWatch,
    this.failure,
    super.key,
  });

  final Future<void> Function(String cameraName) onCamera;
  final Future<void> Function() onWatch;

  /// Why the last pairing attempt failed, if it did.
  final PairingFailure? failure;

  @override
  State<FirstRunScreen> createState() => _FirstRunScreenState();
}

class _FirstRunScreenState extends State<FirstRunScreen> {
  final _name = TextEditingController();
  bool _nameInitialized = false;
  List<DeviceNameError> _nameErrors = const [];

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_nameInitialized) return;
    _nameInitialized = true;
    _name.text = AppLocalizations.of(context).defaultCameraName;
  }

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  Future<void> _useAsCamera() async {
    final errors = DeviceNameValidator.validate(_name.text);
    setState(() => _nameErrors = errors);
    if (errors.isNotEmpty) return;
    await widget.onCamera(_name.text);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final failure = widget.failure;
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              l10n.firstRunTitle,
              textAlign: TextAlign.center,
              style: theme.textTheme.headlineSmall,
            ),
            const SizedBox(height: 24),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    FilledButton.icon(
                      onPressed: widget.onWatch,
                      icon: const Icon(Icons.live_tv),
                      label: Text(l10n.firstRunWatch),
                    ),
                    const SizedBox(height: 8),
                    Text(l10n.firstRunWatchHint, textAlign: TextAlign.center),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    TextField(
                      controller: _name,
                      decoration: InputDecoration(
                        labelText: l10n.cameraNameLabel,
                        border: const OutlineInputBorder(),
                        errorText: _nameErrors.isEmpty
                            ? null
                            : _nameErrors
                                  .map(
                                    (error) => deviceNameErrorText(l10n, error),
                                  )
                                  .join('\n'),
                      ),
                    ),
                    const SizedBox(height: 12),
                    FilledButton.icon(
                      onPressed: _useAsCamera,
                      icon: const Icon(Icons.videocam),
                      label: Text(l10n.firstRunCamera),
                    ),
                    const SizedBox(height: 8),
                    Text(l10n.firstRunCameraHint, textAlign: TextAlign.center),
                  ],
                ),
              ),
            ),
            if (failure != null) ...[
              const SizedBox(height: 16),
              Text(
                pairingFailureText(l10n, failure),
                textAlign: TextAlign.center,
                style: TextStyle(color: theme.colorScheme.error),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
