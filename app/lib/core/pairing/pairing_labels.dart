import '../../l10n/generated/app_localizations.dart';
import 'device_name_validator.dart';
import 'pairing_service.dart';

String pairingFailureText(AppLocalizations l10n, PairingFailure failure) =>
    switch (failure) {
      PairingFailure.invalidQr => l10n.pairingErrorInvalidQr,
      PairingFailure.serverUnreachable => l10n.pairingErrorUnreachable,
      PairingFailure.tokenRejected => l10n.pairingErrorTokenRejected,
      PairingFailure.wrongRole => l10n.pairingErrorWrongRole,
      PairingFailure.pairingLost => l10n.pairingErrorPairingLost,
      PairingFailure.invalidInput ||
      PairingFailure.unexpected => l10n.pairingErrorUnexpected,
    };

String deviceNameErrorText(AppLocalizations l10n, DeviceNameError error) =>
    switch (error) {
      DeviceNameError.empty => l10n.deviceNameEmpty,
      DeviceNameError.tooLong => l10n.deviceNameTooLong(
        DeviceNameValidator.maxLength,
      ),
      DeviceNameError.controlCharacters => l10n.deviceNameInvalidCharacters,
    };
