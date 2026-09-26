import 'package:flutter/material.dart';

import 'core/pairing/device_name_validator.dart';
import 'core/pairing/pairing_labels.dart';
import 'core/pairing/pairing_link.dart';
import 'core/pairing/pairing_service.dart';
import 'l10n/generated/app_localizations.dart';

/// What a phone with nothing paired shows: one button to read the QR code the browser (or a
/// Monitor) shows. The code decides what the phone becomes: a camera code asks for a name
/// first; a Monitor code pairs right away.
class FirstRunScreen extends StatefulWidget {
  const FirstRunScreen({
    required this.readCode,
    required this.onPair,
    this.failure,
    this.initialCode,
    super.key,
  });

  /// Opens the QR reader; null when the person gives up.
  final Future<String?> Function() readCode;

  /// Pairs with the code; the camera name is null for a Monitor code.
  final Future<void> Function(String code, String? cameraName) onPair;

  /// Why the last pairing attempt failed, if it did.
  final PairingFailure? failure;

  /// A code read before the screen opened, as when the phone switches roles: it is handled as
  /// if it had just been scanned.
  final String? initialCode;

  @override
  State<FirstRunScreen> createState() => _FirstRunScreenState();
}

class _FirstRunScreenState extends State<FirstRunScreen> {
  final _name = TextEditingController();
  bool _nameInitialized = false;
  List<DeviceNameError> _nameErrors = const [];

  /// A camera code waiting for its name.
  String? _cameraCode;
  bool _wrongCode = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_nameInitialized) return;
    _nameInitialized = true;
    _name.text = AppLocalizations.of(context).defaultCameraName;
  }

  @override
  void initState() {
    super.initState();
    final code = widget.initialCode;
    if (code != null) {
      WidgetsBinding.instance.addPostFrameCallback((_) => _handle(code));
    }
  }

  @override
  void dispose() {
    _name.dispose();
    super.dispose();
  }

  Future<void> _scan() async {
    final code = await widget.readCode();
    if (code == null) return;
    await _handle(code);
  }

  Future<void> _handle(String code) async {
    if (!mounted) return;
    switch (pairingLinkTarget(code)) {
      case null:
        setState(() => _wrongCode = true);
      case PairingLinkTarget.camera:
        setState(() {
          _wrongCode = false;
          _cameraCode = code;
        });
      case PairingLinkTarget.viewer:
        setState(() => _wrongCode = false);
        await widget.onPair(code, null);
    }
  }

  Future<void> _confirmName() async {
    final code = _cameraCode;
    final errors = DeviceNameValidator.validate(_name.text);
    setState(() => _nameErrors = errors);
    if (code == null || errors.isNotEmpty) return;
    await widget.onPair(code, _name.text);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final failure = _wrongCode ? PairingFailure.invalidQr : widget.failure;
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              _cameraCode == null ? l10n.firstRunTitle : l10n.firstRunNameTitle,
              textAlign: TextAlign.center,
              style: theme.textTheme.headlineSmall,
            ),
            const SizedBox(height: 16),
            if (_cameraCode == null) ...[
              Text(l10n.firstRunIntro, textAlign: TextAlign.center),
              const SizedBox(height: 24),
              FilledButton.icon(
                onPressed: _scan,
                icon: const Icon(Icons.qr_code_scanner),
                label: Text(l10n.scanQrButton),
              ),
            ] else ...[
              Text(l10n.firstRunNameHint, textAlign: TextAlign.center),
              const SizedBox(height: 16),
              TextField(
                controller: _name,
                autofocus: true,
                decoration: InputDecoration(
                  labelText: l10n.cameraNameLabel,
                  border: const OutlineInputBorder(),
                  errorText: _nameErrors.isEmpty
                      ? null
                      : _nameErrors
                            .map((error) => deviceNameErrorText(l10n, error))
                            .join('\n'),
                ),
              ),
              const SizedBox(height: 12),
              FilledButton.icon(
                onPressed: _confirmName,
                icon: const Icon(Icons.videocam),
                label: Text(l10n.firstRunNameConfirm),
              ),
            ],
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
