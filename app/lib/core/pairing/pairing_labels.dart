import '../../l10n/generated/app_localizations.dart';
import 'device_name_validator.dart';
import 'device_role.dart';
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

String deviceRoleText(AppLocalizations l10n, DeviceRole role) => switch (role) {
  DeviceRole.owner => l10n.roleOwner,
  DeviceRole.viewer => l10n.roleViewer,
  DeviceRole.camera => l10n.roleCamera,
};

String deviceNameErrorText(AppLocalizations l10n, DeviceNameError error) =>
    switch (error) {
      DeviceNameError.empty => l10n.deviceNameEmpty,
      DeviceNameError.tooLong => l10n.deviceNameTooLong(
        DeviceNameValidator.maxLength,
      ),
      DeviceNameError.controlCharacters => l10n.deviceNameInvalidCharacters,
    };
