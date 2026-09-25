import 'package:flutter/material.dart';

import '../../l10n/generated/app_localizations.dart';

/// Asks for a pairing code typed or pasted as text. Returns the trimmed text,
/// or null when cancelled or left empty.
Future<String?> showPairingCodeDialog(BuildContext context) async {
  final text = await showDialog<String>(
    context: context,
    builder: (_) => const PairingCodeDialog(),
  );
  final trimmed = text?.trim();
  return (trimmed == null || trimmed.isEmpty) ? null : trimmed;
}

class PairingCodeDialog extends StatefulWidget {
  const PairingCodeDialog({super.key});

  @override
  State<PairingCodeDialog> createState() => _PairingCodeDialogState();
}

class _PairingCodeDialogState extends State<PairingCodeDialog> {
  final _field = TextEditingController();

  @override
  void dispose() {
    _field.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return AlertDialog(
      title: Text(l10n.pasteCodeTitle),
      content: TextField(
        controller: _field,
        autofocus: true,
        minLines: 2,
        maxLines: 4,
        decoration: InputDecoration(hintText: l10n.pasteCodeHint),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: Text(l10n.cancelButton),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(_field.text),
          child: Text(l10n.pasteCodeConfirm),
        ),
      ],
    );
  }
}
