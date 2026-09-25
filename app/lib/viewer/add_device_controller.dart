import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/network/api_client.dart';
import '../core/pairing/device_role.dart';
import '../core/storage/credential_store.dart';

sealed class AddDeviceState {}

final class AddDeviceLoading extends AddDeviceState {}

final class AddDeviceReady extends AddDeviceState {
  AddDeviceReady({required this.qrUri, required this.remaining});

  final String qrUri;
  final Duration remaining;
}

final class AddDeviceFailed extends AddDeviceState {
  AddDeviceFailed(this.kind);

  final ApiFailureKind kind;
}

/// Shows a QR code that pairs another phone as [role], and replaces it with a new one when it
/// expires.
class AddDeviceController extends ChangeNotifier {
  AddDeviceController({
    required this._api,
    required this._session,
    required this.role,
  });

  static const _tick = Duration(seconds: 1);

  final ApiClient _api;
  final PairedSession _session;

  /// Camera or viewer.
  final DeviceRole role;
  AddDeviceState _state = AddDeviceLoading();
  Timer? _timer;
  bool _disposed = false;

  AddDeviceState get state => _state;

  Future<void> start() async {
    _timer?.cancel();
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
          AddDeviceReady(qrUri: value.qrUri, remaining: value.validFor),
        );
        _timer = Timer.periodic(_tick, (_) => _onTick());
      case ApiFailure(:final kind):
        _setState(AddDeviceFailed(kind));
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
    _setState(AddDeviceReady(qrUri: state.qrUri, remaining: remaining));
  }

  void _setState(AddDeviceState state) {
    _state = state;
    notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    _timer?.cancel();
    super.dispose();
  }
}
