part of 'fakes.dart';

class FakeHubClient implements HubClient {
  /// Results for the next connect calls; when empty, connect succeeds.
  final List<bool> connectResults = [];
  final List<({String method, List<Object> args})> invocations = [];
  final Map<String, void Function(List<Object?> args)> handlers = {};
  int connectCalls = 0;
  bool disconnected = false;
  Object? invokeResult;
  bool heartbeatResult = true;
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
  Future<bool> heartbeat() async => heartbeatResult;

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

class FakePublisher implements WebRtcPublisher {
  bool startResult = true;
  int starts = 0;
  int stops = 0;

  @override
  Future<bool> start() async {
    starts++;
    return startResult;
  }

  @override
  Future<void> stop() async => stops++;
}

class FakeViewer implements WebRtcViewer {
  /// Results for the next start calls; when empty, start fails.
  final List<bool> startResults = [];
  int starts = 0;
  int stops = 0;
  bool disposed = false;
  void Function()? ended;

  @override
  set onEnded(void Function() callback) => ended = callback;

  @override
  Future<bool> start() async {
    starts++;
    return startResults.isEmpty ? false : startResults.removeAt(0);
  }

  @override
  Future<void> stop() async => stops++;

  @override
  Widget buildVideo() => const SizedBox.shrink();

  @override
  Future<void> dispose() async => disposed = true;
}

/// Lets queued async work (microtasks and zero-length delays) finish.
Future<void> settle() async {
  for (var i = 0; i < 5; i++) {
    await Future<void>.delayed(Duration.zero);
  }
}
