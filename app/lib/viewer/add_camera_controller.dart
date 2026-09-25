import 'dart:async';

import 'package:flutter/foundation.dart';

import '../core/network/api_client.dart';
import '../core/storage/credential_store.dart';

sealed class AddCameraState {}

final class AddCameraLoading extends AddCameraState {}

final class AddCameraReady extends AddCameraState {
  AddCameraReady({required this.qrUri, required this.remaining});

  final String qrUri;
  final Duration remaining;
}

final class AddCameraFailed extends AddCameraState {
  AddCameraFailed(this.kind);

  final ApiFailureKind kind;
}

/// Shows a camera pairing QR code and replaces it with a new one when it expires.
class AddCameraController extends ChangeNotifier {
  AddCameraController({required this._api, required this._session});

  static const _tick = Duration(seconds: 1);

  final ApiClient _api;
  final PairedSession _session;
  AddCameraState _state = AddCameraLoading();
  Timer? _timer;
  bool _disposed = false;

  AddCameraState get state => _state;

  Future<void> start() async {
    _timer?.cancel();
    _setState(AddCameraLoading());
    final result = await _api.createCameraPairingToken(
      _session.serverUrl,
      _session.credential,
    );
    if (_disposed) return;
    switch (result) {
      case ApiSuccess(:final value):
        _setState(
          AddCameraReady(qrUri: value.qrUri, remaining: value.validFor),
        );
        _timer = Timer.periodic(_tick, (_) => _onTick());
      case ApiFailure(:final kind):
        _setState(AddCameraFailed(kind));
    }
  }

  void _onTick() {
    final state = _state;
    if (state is! AddCameraReady) return;
    final remaining = state.remaining - _tick;
    if (remaining <= Duration.zero) {
      unawaited(start());
      return;
    }
    _setState(AddCameraReady(qrUri: state.qrUri, remaining: remaining));
  }

  void _setState(AddCameraState state) {
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
