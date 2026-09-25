part of 'fakes.dart';

class FakeHubClient implements HubClient {
  /// Results for the next connect calls; when empty, connect succeeds.
  final List<bool> connectResults = [];
  final List<({String method, List<Object> args})> invocations = [];
  final Map<String, void Function(List<Object?> args)> handlers = {};
  int connectCalls = 0;
  bool disconnected = false;
  Object? invokeResult;
  void Function()? _onClosed;

  @override
  set onClosed(void Function() callback) => _onClosed = callback;

  /// Simulates the server dropping the connection.
  void drop() => _onClosed?.call();

  /// Simulates the server calling a client method.
  void receive(String method, List<Object?> args) => handlers[method]!(args);

  @override
  Future<bool> connect() async {
    connectCalls++;
    return connectResults.isEmpty ? true : connectResults.removeAt(0);
  }

  @override
  Future<void> disconnect() async => disconnected = true;

  @override
  void on(String method, void Function(List<Object?> args) handler) =>
      handlers[method] = handler;

  @override
  Future<Object?> invoke(String method, [List<Object> args = const []]) async {
    invocations.add((method: method, args: args));
    return invokeResult;
  }
}

class FakeBatteryReader implements BatteryReader {
  BatteryReading reading = const BatteryReading(level: 80, isCharging: true);

  @override
  Future<BatteryReading> read() async => reading;
}

class FakeScreenController implements ScreenController {
  bool inCameraMode = false;

  @override
  Future<void> enterCameraMode() async => inCameraMode = true;

  @override
  Future<void> exitCameraMode() async => inCameraMode = false;
}

class FakeKeepAlive implements KeepAlive {
  bool running = false;

  @override
  Future<void> start({required String title, required String text}) async =>
      running = true;

  @override
  Future<void> stop() async => running = false;
}

/// Lets queued async work (microtasks and zero-length delays) finish.
Future<void> settle() async {
  for (var i = 0; i < 5; i++) {
    await Future<void>.delayed(Duration.zero);
  }
}
