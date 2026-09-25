import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/media/recording_relay.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/core/storage/credential_store.dart';

/// Plays the ReCam server: serves one recording, honoring Range.
class _FakeServer {
  final List<HttpHeaders> requests = [];
  late final HttpServer _server;
  final List<int> bytes = List.generate(100, (index) => index);

  Uri get url => Uri.parse('http://127.0.0.1:${_server.port}');

  Future<void> start() async {
    _server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    _server.listen((request) async {
      requests.add(request.headers);
      final response = request.response;
      final range = request.headers.value(HttpHeaders.rangeHeader);
      if (range == 'bytes=10-19') {
        response
          ..statusCode = HttpStatus.partialContent
          ..headers.set(HttpHeaders.contentRangeHeader, 'bytes 10-19/100')
          ..headers.contentType = ContentType('video', 'mp4')
          ..add(bytes.sublist(10, 20));
      } else {
        response
          ..headers.contentType = ContentType('video', 'mp4')
          ..add(bytes);
      }
      await response.close();
    });
  }

  Future<void> stop() => _server.close(force: true);
}

void main() {
  late _FakeServer server;
  late RecordingRelay relay;

  setUp(() async {
    server = _FakeServer();
    await server.start();
    relay = RecordingRelay(
      session: PairedSession(
        serverUrl: server.url,
        credential: 'id.secret',
        deviceId: 'id',
        role: DeviceRole.owner,
        serverName: 'ReCam',
      ),
    );
  });

  tearDown(() async {
    await relay.close();
    await server.stop();
  });

  Future<HttpClientResponse> get(Uri url, {String? range}) async {
    final client = HttpClient();
    final request = await client.getUrl(url);
    if (range != null) request.headers.set(HttpHeaders.rangeHeader, range);
    final response = await request.close();
    client.close();
    return response;
  }

  group('RecordingRelay', () {
    test('withRange_forwardsItWithTheCredential', () async {
      // arrange
      final url = await relay.urlFor('/api/recordings/cam/2026.mp4');

      // act
      final response = await get(url, range: 'bytes=10-19');
      final body = await response.fold<List<int>>(
        [],
        (all, chunk) => all..addAll(chunk),
      );

      // assert
      expect(response.statusCode, HttpStatus.partialContent);
      expect(
        response.headers.value(HttpHeaders.contentRangeHeader),
        'bytes 10-19/100',
      );
      expect(body, List.generate(10, (index) => index + 10));
      final upstream = server.requests.single;
      expect(
        upstream.value(HttpHeaders.authorizationHeader),
        'Bearer id.secret',
      );
      expect(upstream.value(HttpHeaders.rangeHeader), 'bytes=10-19');
    });

    test('withoutTheSecretPath_refuses', () async {
      // arrange
      final url = await relay.urlFor('/api/recordings/cam/2026.mp4');
      final guessed = url.replace(path: '/api/recordings/cam/2026.mp4');

      // act
      final response = await get(guessed);
      await response.drain<void>();

      // assert
      expect(response.statusCode, HttpStatus.notFound);
      expect(server.requests, isEmpty);
    });

    test('forOtherServerPaths_refuses', () async {
      // arrange
      final url = await relay.urlFor('/api/me');

      // act
      final response = await get(url);
      await response.drain<void>();

      // assert
      expect(response.statusCode, HttpStatus.notFound);
      expect(server.requests, isEmpty);
    });

    test('listensOnlyOnThisPhone', () async {
      // arrange
      final url = await relay.urlFor('/api/recordings/cam/2026.mp4');

      // act
      final address = relay.address;

      // assert
      expect(address?.isLoopback, isTrue);
      expect(url.host, '127.0.0.1');
    });
  });
}
