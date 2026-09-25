import 'dart:async';
import 'dart:io';
import 'dart:math';

import '../storage/credential_store.dart';

/// Where the recording player fetches a segment from.
abstract interface class SegmentSource {
  /// A URL the local player can open for a server path like `/api/recordings/...`.
  Future<Uri> urlFor(String serverPath);

  Future<void> close();
}

/// Hands recordings to Android's video player, which knows neither the pinned certificate nor
/// the device credential. Listens on 127.0.0.1 only, under a random secret path, and streams each
/// request from the paired server with the credential and the pinning of `dart:io`, `Range`
/// included. Nothing is stored on the phone.
class RecordingRelay implements SegmentSource {
  RecordingRelay({required this._session, HttpClient Function()? httpClient})
    : _newClient = httpClient ?? HttpClient.new;

  static const _allowedPrefix = '/api/recordings/';
  static const _forwardedHeaders = [
    HttpHeaders.contentTypeHeader,
    HttpHeaders.contentRangeHeader,
    HttpHeaders.acceptRangesHeader,
  ];

  final PairedSession _session;
  final HttpClient Function() _newClient;
  final String _secret = _randomSecret();
  HttpServer? _server;

  /// The address the relay listens on; loopback, so other devices cannot reach it.
  InternetAddress? get address => _server?.address;

  @override
  Future<Uri> urlFor(String serverPath) async {
    final server = _server ??= await _start();
    return Uri(
      scheme: 'http',
      host: server.address.address,
      port: server.port,
      path: '/$_secret$serverPath',
    );
  }

  @override
  Future<void> close() async {
    await _server?.close(force: true);
    _server = null;
  }

  Future<HttpServer> _start() async {
    final server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    server.listen((request) => unawaited(_handle(request)));
    return server;
  }

  Future<void> _handle(HttpRequest request) async {
    final response = request.response;
    final path = request.uri.path;
    final prefix = '/$_secret';
    final fromThisPhone =
        request.connectionInfo?.remoteAddress.isLoopback ?? false;
    if (!fromThisPhone ||
        request.method != 'GET' ||
        !path.startsWith('$prefix$_allowedPrefix')) {
      response.statusCode = HttpStatus.notFound;
      await response.close();
      return;
    }

    final client = _newClient();
    // Network failures arrive as exceptions of several types; the player just sees a failed load.
    try {
      final upstream = await client.getUrl(
        _session.serverUrl.resolve(path.substring(prefix.length)),
      );
      upstream.headers.set(
        HttpHeaders.authorizationHeader,
        'Bearer ${_session.credential}',
      );
      final range = request.headers.value(HttpHeaders.rangeHeader);
      if (range != null) upstream.headers.set(HttpHeaders.rangeHeader, range);
      final reply = await upstream.close();
      response.statusCode = reply.statusCode;
      for (final name in _forwardedHeaders) {
        final value = reply.headers.value(name);
        if (value != null) response.headers.set(name, value);
      }
      if (reply.contentLength >= 0) {
        response.contentLength = reply.contentLength;
      }
      await reply.pipe(response);
    } on Object {
      response.statusCode = HttpStatus.badGateway;
      await response.close();
    } finally {
      client.close();
    }
  }

  static String _randomSecret() {
    final random = Random.secure();
    return List.generate(
      16,
      (_) => random.nextInt(256).toRadixString(16).padLeft(2, '0'),
    ).join();
  }
}
