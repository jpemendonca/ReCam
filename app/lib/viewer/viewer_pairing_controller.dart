import 'package:flutter/foundation.dart';

import '../core/pairing/device_role.dart';
import '../core/pairing/pairing_service.dart';
import '../core/storage/credential_store.dart';

sealed class ViewerPairingState {}

final class ViewerPairingLoading extends ViewerPairingState {}

final class ViewerNotPaired extends ViewerPairingState {
  ViewerNotPaired({this.lastFailure});

  final PairingFailure? lastFailure;
}

final class ViewerPairing extends ViewerPairingState {}

final class ViewerPaired extends ViewerPairingState {
  ViewerPaired(this.session);

  final PairedSession session;
}

class ViewerPairingController extends ChangeNotifier {
  ViewerPairingController({required this._pairing});

  static const _slot = PairingSlot.viewer;
  static const _acceptedRoles = {DeviceRole.owner, DeviceRole.viewer};

  final PairingService _pairing;
  ViewerPairingState _state = ViewerPairingLoading();

  ViewerPairingState get state => _state;

  Future<void> load() async {
    final session = await _pairing.restore(_slot);
    if (session == null) {
      _setState(ViewerNotPaired());
      return;
    }
    _setState(ViewerPaired(session));
    final status = await _pairing.verify(_slot, session);
    if (status == SessionStatus.revoked ||
        status == SessionStatus.serverChanged) {
      _setState(ViewerNotPaired(lastFailure: PairingFailure.pairingLost));
    }
  }

  /// Forgets the pairing on this phone, as if it had never paired.
  Future<void> reset() async {
    await _pairing.forget(_slot);
    _setState(ViewerNotPaired());
  }

  /// Forgets a pairing the server no longer accepts and asks for a new one.
  Future<void> forget() async {
    await _pairing.forget(_slot);
    _setState(ViewerNotPaired(lastFailure: PairingFailure.pairingLost));
  }

  Future<void> submitQr(String rawQr, {required String deviceName}) async {
    if (_state is ViewerPairing) return;
    _setState(ViewerPairing());
    final outcome = await _pairing.pairFromQr(
      rawQr: rawQr,
      deviceName: deviceName,
      slot: _slot,
      acceptedRoles: _acceptedRoles,
    );
    _setState(switch (outcome) {
      PairingSucceeded(:final session) => ViewerPaired(session),
      PairingFailed(:final reason) => ViewerNotPaired(lastFailure: reason),
    });
  }

  void _setState(ViewerPairingState state) {
    _state = state;
    notifyListeners();
  }
}
