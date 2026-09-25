import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../l10n/generated/app_localizations.dart';
import 'pairing_code_dialog.dart';

/// Full-screen QR reader. Pops with the raw text of the first QR code it sees,
/// or with a code pasted as text for phones that cannot scan.
class QrScannerScreen extends StatefulWidget {
  const QrScannerScreen({super.key});

  @override
  State<QrScannerScreen> createState() => _QrScannerScreenState();
}

class _QrScannerScreenState extends State<QrScannerScreen> {
  final _controller = MobileScannerController(
    formats: const [BarcodeFormat.qrCode],
  );
  bool _done = false;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _onDetect(BarcodeCapture capture) {
    if (_done) return;
    final value = capture.barcodes
        .map((barcode) => barcode.rawValue)
        .nonNulls
        .firstOrNull;
    if (value == null) return;
    _done = true;
    Navigator.of(context).pop(value);
  }

  Future<void> _pasteCode() async {
    final code = await showPairingCodeDialog(context);
    if (code == null || _done || !mounted) return;
    _done = true;
    Navigator.of(context).pop(code);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.scanQrTitle),
        actions: [
          TextButton.icon(
            onPressed: _pasteCode,
            icon: const Icon(Icons.content_paste),
            label: Text(l10n.pasteCodeButton),
          ),
        ],
      ),
      body: MobileScanner(
        controller: _controller,
        onDetect: _onDetect,
        errorBuilder: (context, error) => Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Text(l10n.scannerUnavailable, textAlign: TextAlign.center),
          ),
        ),
      ),
    );
  }
}
