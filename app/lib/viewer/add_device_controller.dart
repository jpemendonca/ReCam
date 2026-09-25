import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/network/api_client.dart';
import '../core/pairing/device_role.dart';
import '../core/storage/credential_store.dart';

sealed class AddDeviceState {}

final class AddDeviceLoading extends AddDeviceState {}

final class AddDeviceReady extends AddDeviceState {
  AddDeviceReady({
    required this.tokenId,
    required this.qrUri,
    required this.remaining,
  });

  final String tokenId;
  final String qrUri;
  final Duration remaining;
}

final class AddDeviceFailed extends AddDeviceState {
  AddDeviceFailed(this.kind);

  final ApiFailureKind kind;
}

/// Another phone paired with the QR code; the screen can close.
final class AddDevicePaired extends AddDeviceState {}

/// Shows a QR code that pairs another phone as [role], replaces it with a new one when it
/// expires, and asks the server every [pollInterval] whether someone already paired with it.
class AddDeviceController extends ChangeNotifier {
  AddDeviceController({
    required this._api,
    required this._session,
    required this.role,
    this.pollInterval = const Duration(seconds: 2),
  });

  static const _tick = Duration(seconds: 1);

  final ApiClient _api;
  final PairedSession _session;

  /// Camera or viewer.
  final DeviceRole role;
  final Duration pollInterval;
  AddDeviceState _state = AddDeviceLoading();
  Timer? _timer;
  Timer? _pollTimer;
  bool _polling = false;
  bool _disposed = false;

  AddDeviceState get state => _state;

  Future<void> start() async {
    _stopTimers();
    _setState(AddDeviceLoading());
    final result = await _api.createPairingToken(
      _session.serverUrl,
      _session.credential,
      role,
    );
    if (_disposed) return;
    switch (result) {
      case ApiSuccess(:final value):
        _setState(
          AddDeviceReady(
            tokenId: value.id,
            qrUri: value.qrUri,
            remaining: value.validFor,
          ),
        );
        _timer = Timer.periodic(_tick, (_) => _onTick());
        _pollTimer = Timer.periodic(
          pollInterval,
          (_) => unawaited(checkPaired()),
        );
      case ApiFailure(:final kind):
        _setState(AddDeviceFailed(kind));
    }
  }

  /// Asks the server whether the QR code shown was used. The poll timer calls this.
  Future<void> checkPaired() async {
    final state = _state;
    if (state is! AddDeviceReady || _polling) return;
    _polling = true;
    final result = await _api.pairingTokenUsed(
      _session.serverUrl,
      _session.credential,
      state.tokenId,
    );
    _polling = false;
    if (_disposed || _state is! AddDeviceReady) return;
    if (result case ApiSuccess(value: true)) {
      _stopTimers();
      _setState(AddDevicePaired());
    }
  }

  void _onTick() {
    final state = _state;
    if (state is! AddDeviceReady) return;
    final remaining = state.remaining - _tick;
    if (remaining <= Duration.zero) {
      unawaited(start());
      return;
    }
    _setState(
      AddDeviceReady(
        tokenId: state.tokenId,
        qrUri: state.qrUri,
        remaining: remaining,
      ),
    );
  }

  void _stopTimers() {
    _timer?.cancel();
    _timer = null;
    _pollTimer?.cancel();
    _pollTimer = null;
  }

  void _setState(AddDeviceState state) {
    _state = state;
    notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    _stopTimers();
    super.dispose();
  }
}
