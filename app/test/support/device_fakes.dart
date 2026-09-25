part of 'fakes.dart';

class FakeHubClient implements HubClient {
  /// Results for the next connect calls; when empty, connect succeeds.
  final List<HubConnectOutcome> connectResults = [];
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
  Future<HubConnectOutcome> connect() async {
    connectCalls++;
    return connectResults.isEmpty
        ? HubConnectOutcome.connected
        : connectResults.removeAt(0);
  }

  @override
  Future<void> disconnect() async => disconnected = true;

  @override
  void on(String method, void Function(List<Object?> args) handler) =>
      handlers[method] = handler;

  @override
  void off(String method) => handlers.remove(method);

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

class FakeBatteryOptimization implements BatteryOptimization {
  bool ignored = true;
  String maker = 'samsung';
  int requests = 0;
  int settingsOpened = 0;

  @override
  Future<bool> isIgnored() async => ignored;

  @override
  Future<String> manufacturer() async => maker;

  @override
  Future<void> requestIgnore() async => requests++;

  @override
  Future<void> openAppSettings() async => settingsOpened++;
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
  bool torchResult = true;
  int starts = 0;
  int stops = 0;
  final List<bool> torchCalls = [];
  final List<VideoQuality> qualities = [];

  @override
  Future<void> setQuality(VideoQuality quality) async => qualities.add(quality);

  bool h264 = true;

  @override
  Future<bool> canSendH264() async => h264;

  /// The feed the last successful start published.
  CameraFeed? publishedFeed;

  @override
  Future<bool> setTorch(bool on) async {
    torchCalls.add(on);
    return torchResult;
  }

  @override
  Future<bool> start(CameraFeed feed) async {
    starts++;
    if (startResult) publishedFeed = feed;
    return startResult;
  }

  @override
  Future<void> stop() async {
    stops++;
    publishedFeed = null;
  }
}

class FakeRecordingPlayer implements RecordingPlayer {
  final List<({Uri url, Duration from})> plays = [];
  bool disposed = false;
  void Function()? _finished;

  /// Simulates the current segment reaching its end.
  void finish() => _finished?.call();

  @override
  set onFinished(void Function() callback) => _finished = callback;

  @override
  Future<void> play(Uri url, {Duration from = Duration.zero}) async =>
      plays.add((url: url, from: from));

  @override
  Widget buildVideo() => const SizedBox(key: Key('recording-video'));

  @override
  Future<void> dispose() async => disposed = true;
}

class FakeSegmentSource implements SegmentSource {
  bool closed = false;

  @override
  Future<Uri> urlFor(String serverPath) async =>
      Uri.parse('http://127.0.0.1:1/relay$serverPath');

  @override
  Future<void> close() async => closed = true;
}

class FakeFeed implements CameraFeed {
  @override
  Widget buildPreview() => const SizedBox(key: Key('camera-preview'));
}

class FakeCapture implements CameraCapture {
  bool available = true;
  int opens = 0;
  int closes = 0;
  FakeFeed? feed;

  bool get isOpen => feed != null;

  @override
  Future<CameraFeed?> open() async {
    if (!available) return null;
    opens++;
    return feed = FakeFeed();
  }

  @override
  Future<void> close() async {
    closes++;
    feed = null;
  }
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
