import 'package:flutter/foundation.dart';

import '../core/pairing/device_name_validator.dart';
import '../core/pairing/device_role.dart';
import '../core/pairing/pairing_service.dart';
import '../core/storage/credential_store.dart';

sealed class CameraPairingState {}

final class CameraPairingLoading extends CameraPairingState {}

final class CameraNotPaired extends CameraPairingState {
  CameraNotPaired({this.lastFailure, this.nameErrors = const []});

  final PairingFailure? lastFailure;
  final List<DeviceNameError> nameErrors;
}

final class CameraPairing extends CameraPairingState {}

final class CameraPaired extends CameraPairingState {
  CameraPaired(this.session);

  final PairedSession session;
}

class CameraPairingController extends ChangeNotifier {
  CameraPairingController({required this._pairing});

  static const _slot = PairingSlot.camera;
  static const _acceptedRoles = {DeviceRole.camera};

  final PairingService _pairing;
  CameraPairingState _state = CameraPairingLoading();

  CameraPairingState get state => _state;

  Future<void> load() async {
    final session = await _pairing.restore(_slot);
    if (session == null) {
      _setState(CameraNotPaired());
      return;
    }
    _setState(CameraPaired(session));
    if (await _pairing.verify(_slot, session) == SessionStatus.revoked) {
      _setState(CameraNotPaired());
    }
  }

  /// Checks the camera name before the QR scan starts. Returns false and shows the
  /// problems when the name cannot be used.
  bool checkName(String name) {
    final errors = DeviceNameValidator.validate(name);
    if (errors.isNotEmpty) _setState(CameraNotPaired(nameErrors: errors));
    return errors.isEmpty;
  }

  Future<void> submitQr(String rawQr, {required String name}) async {
    if (_state is CameraPairing) return;
    _setState(CameraPairing());
    final outcome = await _pairing.pairFromQr(
      rawQr: rawQr,
      deviceName: name.trim(),
      slot: _slot,
      acceptedRoles: _acceptedRoles,
    );
    _setState(switch (outcome) {
      PairingSucceeded(:final session) => CameraPaired(session),
      PairingFailed(:final reason) => CameraNotPaired(lastFailure: reason),
    });
  }

  void _setState(CameraPairingState state) {
    _state = state;
    notifyListeners();
  }
}
