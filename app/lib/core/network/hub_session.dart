import 'dart:async';

import 'package:flutter/foundation.dart';

import 'hub_client.dart';
import 'reconnect_backoff.dart';

typedef Delay = Future<void> Function(Duration duration);

/// Keeps a hub connection open: connects, and after any failure or drop waits with
/// [ReconnectBackoff] and tries again until [stop].
///
/// A drop is noticed two ways: the client's close event, and a heartbeat every
/// [heartbeatInterval]. The heartbeat is needed because the Dart SignalR client does not
/// always report a socket that died silently (a server restart, a Wi-Fi switch).
class HubSession extends ChangeNotifier {
  HubSession({
    required this.client,
    Delay? delay,
    ReconnectBackoff? backoff,
    this.heartbeatInterval = const Duration(seconds: 20),
    this.heartbeatTimeout = const Duration(seconds: 10),
  }) : _delay = delay ?? Future<void>.delayed,
       _backoff = backoff ?? ReconnectBackoff() {
    client.onClosed = _handleClosed;
  }

  final HubClient client;
  final Delay _delay;
  final ReconnectBackoff _backoff;
  final Duration heartbeatInterval;
  final Duration heartbeatTimeout;

  bool _running = false;
  bool _connected = false;
  Completer<void>? _closed;
  Timer? _heartbeatTimer;

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
    _stopHeartbeat();
    _completeClosed();
    await client.disconnect();
    _setConnected(false);
  }

  /// Sends one heartbeat; when it fails, drops the connection so the session reconnects.
  /// The timer calls this on its own; tests call it directly.
  Future<void> checkConnection() async {
    if (!_connected) return;
    final alive = await client.heartbeat().timeout(
      heartbeatTimeout,
      onTimeout: () => false,
    );
    if (alive || !_connected) return;
    // A dead socket can make the client's own stop hang; the round ends regardless.
    await client.disconnect().timeout(heartbeatTimeout, onTimeout: () {});
    _completeClosed();
  }

  Future<void> _run() async {
    while (_running) {
      // Created before connecting, so a close that fires right after the handshake still
      // ends this round instead of being lost.
      final closed = Completer<void>();
      _closed = closed;
      if (await client.connect()) {
        if (!_running) {
          await client.disconnect();
          return;
        }
        _backoff.reset();
        _setConnected(true);
        _startHeartbeat();
        onConnected?.call();
        await closed.future;
        _stopHeartbeat();
        _setConnected(false);
      }
      if (_running) await _delay(_backoff.next());
    }
  }

  void _startHeartbeat() {
    _stopHeartbeat();
    _heartbeatTimer = Timer.periodic(
      heartbeatInterval,
      (_) => unawaited(checkConnection()),
    );
  }

  void _stopHeartbeat() {
    _heartbeatTimer?.cancel();
    _heartbeatTimer = null;
  }

  void _handleClosed() => _completeClosed();

  void _completeClosed() {
    final closed = _closed;
    if (closed != null && !closed.isCompleted) closed.complete();
  }

  void _setConnected(bool value) {
    if (_connected == value) return;
    _connected = value;
    notifyListeners();
  }

  @override
  void dispose() {
    _stopHeartbeat();
    super.dispose();
  }
}
