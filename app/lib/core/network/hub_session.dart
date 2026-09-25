import 'dart:async';

import 'package:flutter/foundation.dart';

import 'hub_client.dart';
import 'reconnect_backoff.dart';

typedef Delay = Future<void> Function(Duration duration);

/// Keeps a hub connection open: connects, and after any failure or drop waits with
/// [ReconnectBackoff] and tries again until [stop].
class HubSession extends ChangeNotifier {
  HubSession({required this.client, Delay? delay, ReconnectBackoff? backoff})
    : _delay = delay ?? Future<void>.delayed,
      _backoff = backoff ?? ReconnectBackoff() {
    client.onClosed = _handleClosed;
  }

  final HubClient client;
  final Delay _delay;
  final ReconnectBackoff _backoff;

  bool _running = false;
  bool _connected = false;
  Completer<void>? _closed;

  /// Runs every time the connection opens, including reconnections.
  VoidCallback? onConnected;

  bool get connected => _connected;

  void start() {
    if (_running) return;
    _running = true;
    unawaited(_run());
  }

  Future<void> stop() async {
    _running = false;
    _closed?.complete();
    _closed = null;
    await client.disconnect();
    _setConnected(false);
  }

  Future<void> _run() async {
    while (_running) {
      if (await client.connect()) {
        if (!_running) {
          await client.disconnect();
          return;
        }
        _backoff.reset();
        final closed = Completer<void>();
        _closed = closed;
        _setConnected(true);
        onConnected?.call();
        await closed.future;
        _setConnected(false);
      }
      if (_running) await _delay(_backoff.next());
    }
  }

  void _handleClosed() {
    final closed = _closed;
    _closed = null;
    if (closed != null && !closed.isCompleted) closed.complete();
  }

  void _setConnected(bool value) {
    if (_connected == value) return;
    _connected = value;
    notifyListeners();
  }
}
