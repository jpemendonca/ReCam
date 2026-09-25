import 'package:signalr_netcore/signalr_client.dart';

import '../storage/credential_store.dart';

/// One connection to the server hub at /hubs/devices.
abstract interface class HubClient {
  /// Returns false when the server cannot be reached; the caller retries.
  Future<bool> connect();

  Future<void> disconnect();

  /// Called when an open connection drops.
  set onClosed(void Function() callback);

  void on(String method, void Function(List<Object?> args) handler);

  /// Removes every handler of [method].
  void off(String method);

  /// Calls a hub method. Returns null when the call could not complete.
  Future<Object?> invoke(String method, [List<Object> args = const []]);

  /// Proves the connection works end to end. False when the server does not answer.
  Future<bool> heartbeat();
}

/// Whether a hub call returned a successful `HubResult` (SPECS.md 5.6).
bool isHubSuccess(Object? result) =>
    result is Map<String, Object?> && result['ok'] == true;

typedef HubClientFactory = HubClient Function(PairedSession session);

class SignalRHubClient implements HubClient {
  SignalRHubClient(PairedSession session)
    : _connection = HubConnectionBuilder()
          .withUrl(
            session.serverUrl.resolve('/hubs/devices').toString(),
            options: HttpConnectionOptions(
              transport: HttpTransportType.WebSockets,
              accessTokenFactory: () async => session.credential,
            ),
          )
          .build();

  final HubConnection _connection;

  @override
  set onClosed(void Function() callback) =>
      _connection.onclose(({error}) => callback());

  // The SignalR client reports network failures as exceptions of several types; at this
  // boundary they all mean "not connected, try again later".
  @override
  Future<bool> connect() async {
    try {
      await _connection.start();
      return true;
    } on Object {
      return false;
    }
  }

  @override
  Future<void> disconnect() => _connection.stop();

  @override
  void on(String method, void Function(List<Object?> args) handler) =>
      _connection.on(method, (args) => handler(args ?? const []));

  @override
  void off(String method) => _connection.off(method);

  @override
  Future<bool> heartbeat() async => await invoke('Heartbeat') != null;

  // A call can fail when the connection drops mid-flight; the session reconnects and the
  // caller sends again later, so the failure is reported as null.
  @override
  Future<Object?> invoke(String method, [List<Object> args = const []]) async {
    try {
      return await _connection.invoke(method, args: args);
    } on Object {
      return null;
    }
  }
}
