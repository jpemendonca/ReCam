import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import '../pairing/device_role.dart';
import 'api_client.dart';

class HttpApiClient implements ApiClient {
  HttpApiClient(this._client, {this.timeout = const Duration(seconds: 5)});

  final http.Client _client;
  final Duration timeout;

  @override
  Future<String?> serverVersion(Uri baseUrl) async {
    try {
      final response = await _client
          .get(baseUrl.resolve('/health'))
          .timeout(timeout);
      if (response.statusCode != HttpStatus.ok) return null;
      final body = jsonDecode(response.body);
      return body is Map<String, Object?> && body['version'] is String
          ? body['version']! as String
          : null;
    } on IOException {
      return null;
    } on http.ClientException {
      return null;
    } on TimeoutException {
      return null;
    } on FormatException {
      return null;
    }
  }

  @override
  Future<bool> health(Uri baseUrl) async {
    try {
      final response = await _client
          .get(baseUrl.resolve('/health'))
          .timeout(timeout);
      return response.statusCode == HttpStatus.ok;
    } on IOException {
      return false;
    } on http.ClientException {
      return false;
    } on TimeoutException {
      return false;
    }
  }

  @override
  Future<ApiResult<PairResult>> pair(
    Uri baseUrl, {
    required String token,
    required String name,
    required Set<DeviceRole> expectedRoles,
  }) {
    return _send(
      () => _client.post(
        baseUrl.resolve('/api/pair'),
        headers: {HttpHeaders.contentTypeHeader: 'application/json'},
        body: jsonEncode({
          'token': token,
          'name': name,
          'expectedRoles': [for (final role in expectedRoles) role.name],
        }),
      ),
      expectedStatus: HttpStatus.created,
      parse: (json, _) {
        final role = DeviceRole.tryParse(json['role'] as String?);
        if (role == null) return null;
        return PairResult(
          deviceId: json['deviceId'] as String,
          credential: json['credential'] as String,
          role: role,
          serverName: json['serverName'] as String,
        );
      },
    );
  }

  @override
  Future<ApiResult<MeResult>> me(Uri baseUrl, String credential) {
    return _send(
      () => _client.get(
        baseUrl.resolve('/api/me'),
        headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
      ),
      expectedStatus: HttpStatus.ok,
      parse: (json, _) {
        final role = DeviceRole.tryParse(json['role'] as String?);
        if (role == null) return null;
        return MeResult(
          deviceId: json['deviceId'] as String,
          name: json['name'] as String,
          role: role,
        );
      },
    );
  }

  // A credential the server already refuses counts as gone.
  @override
  Future<bool> leave(Uri baseUrl, String credential) async {
    try {
      final response = await _client
          .delete(
            baseUrl.resolve('/api/me'),
            headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
          )
          .timeout(timeout);
      return response.statusCode == HttpStatus.noContent ||
          response.statusCode == HttpStatus.unauthorized;
    } on IOException {
      return false;
    } on http.ClientException {
      return false;
    } on TimeoutException {
      return false;
    }
  }

  @override
  Future<ApiResult<PairingTokenResult>> createPairingToken(
    Uri baseUrl,
    String credential,
    DeviceRole role,
  ) {
    return _send(
      () => _client.post(
        baseUrl.resolve('/api/pairing-tokens'),
        headers: {
          HttpHeaders.contentTypeHeader: 'application/json',
          HttpHeaders.authorizationHeader: 'Bearer $credential',
        },
        body: jsonEncode({'role': role.name}),
      ),
      expectedStatus: HttpStatus.created,
      parse: (json, headers) {
        final id = json['id'];
        final qrUri = json['qrUri'];
        final expiresAt = DateTime.tryParse(json['expiresAt'] as String? ?? '');
        if (id is! String || qrUri is! String || expiresAt == null) return null;
        return PairingTokenResult(
          id: id,
          qrUri: qrUri,
          validFor: _remainingAt(expiresAt, headers),
        );
      },
    );
  }

  // The server clock, not this phone's, decides when the token expires. The Date header
  // tells the server's current time, so a wrong phone clock does not shorten the countdown.
  static Duration _remainingAt(
    DateTime expiresAt,
    Map<String, String> headers,
  ) {
    final date = headers[HttpHeaders.dateHeader];
    final serverNow = date == null ? null : _tryParseHttpDate(date);
    return expiresAt.difference(serverNow ?? DateTime.now());
  }

  static DateTime? _tryParseHttpDate(String value) {
    try {
      return HttpDate.parse(value);
    } on HttpException {
      return null;
    }
  }

  @override
  Future<ApiResult<bool>> pairingTokenUsed(
    Uri baseUrl,
    String credential,
    String tokenId,
  ) {
    return _send(
      () => _client.get(
        baseUrl.resolve('/api/pairing-tokens/${Uri.encodeComponent(tokenId)}'),
        headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
      ),
      expectedStatus: HttpStatus.ok,
      parse: (json, _) => switch (json['used']) {
        final bool used => used,
        _ => null,
      },
    );
  }

  @override
  Future<ApiResult<RecordingQuota>> recordingQuota(
    Uri baseUrl,
    String credential,
  ) {
    return _send(
      () => _client.get(
        baseUrl.resolve('/api/recordings/quota'),
        headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
      ),
      expectedStatus: HttpStatus.ok,
      parse: (json, _) =>
          switch ((json['quotaMb'], json['usedBytes'], json['freeBytes'])) {
            (final int quota, final int used, final int free) => RecordingQuota(
              megabytes: quota,
              usedBytes: used,
              freeBytes: free,
            ),
            _ => null,
          },
    );
  }

  @override
  Future<ApiResult<List<DeviceInfo>>> devices(Uri baseUrl, String credential) {
    return _sendJson(
      () => _client.get(
        baseUrl.resolve('/api/devices'),
        headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
      ),
      expectedStatus: HttpStatus.ok,
      parse: (json, _) {
        if (json is! List<Object?>) return null;
        final devices = [
          for (final item in json)
            if (item case {
              'id': final String id,
              'name': final String name,
              'role': final String role,
              'online': final bool online,
            })
              if (DeviceRole.tryParse(role) case final DeviceRole parsed)
                DeviceInfo(
                  id: id,
                  name: name,
                  role: parsed,
                  online: online,
                  lastSeenAt: switch (item['lastSeenAt']) {
                    final String seen => DateTime.tryParse(seen)?.toUtc(),
                    _ => null,
                  },
                ),
        ];
        return devices.length == json.length ? devices : null;
      },
    );
  }

  @override
  Future<ApiFailureKind?> approveBrowserLink(
    Uri baseUrl,
    String credential,
    String linkId,
    String secret,
  ) async {
    final http.Response response;
    try {
      response = await _client
          .post(
            baseUrl.resolve('/api/browser-links/$linkId/approve'),
            headers: {
              HttpHeaders.authorizationHeader: 'Bearer $credential',
              HttpHeaders.contentTypeHeader: 'application/json',
            },
            body: jsonEncode({'secret': secret}),
          )
          .timeout(timeout);
    } on IOException {
      return ApiFailureKind.unreachable;
    } on http.ClientException {
      return ApiFailureKind.unreachable;
    } on TimeoutException {
      return ApiFailureKind.unreachable;
    }
    return switch (response.statusCode) {
      HttpStatus.noContent => null,
      HttpStatus.unauthorized => ApiFailureKind.unauthorized,
      HttpStatus.notFound || HttpStatus.forbidden => ApiFailureKind.rejected,
      _ => ApiFailureKind.unexpected,
    };
  }

  @override
  Future<ApiFailureKind?> removeDevice(
    Uri baseUrl,
    String credential,
    String deviceId,
  ) async {
    final http.Response response;
    try {
      response = await _client
          .delete(
            baseUrl.resolve('/api/devices/$deviceId'),
            headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
          )
          .timeout(timeout);
    } on IOException {
      return ApiFailureKind.unreachable;
    } on http.ClientException {
      return ApiFailureKind.unreachable;
    } on TimeoutException {
      return ApiFailureKind.unreachable;
    }
    return switch (response.statusCode) {
      HttpStatus.noContent || HttpStatus.notFound => null,
      HttpStatus.unauthorized => ApiFailureKind.unauthorized,
      HttpStatus.conflict => ApiFailureKind.conflict,
      _ => ApiFailureKind.unexpected,
    };
  }

  @override
  Future<ApiResult<List<DateTime>>> recordingDays(
    Uri baseUrl,
    String credential,
    String cameraId,
  ) {
    return _sendJson(
      () => _client.get(
        baseUrl.resolve('/api/cameras/$cameraId/recording-days'),
        headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
      ),
      expectedStatus: HttpStatus.ok,
      parse: (json, _) {
        if (json is! List<Object?>) return null;
        final days = [
          for (final day in json)
            if (day is String) DateTime.tryParse('${day}T00:00:00Z'),
        ];
        return days.contains(null) || days.length != json.length
            ? null
            : days.nonNulls.toList();
      },
    );
  }

  @override
  Future<ApiResult<List<RecordingPieceInfo>>> recordings(
    Uri baseUrl,
    String credential,
    String cameraId,
    DateTime utcDay,
  ) {
    final day = _dayParameter(utcDay);
    return _sendJson(
      () => _client.get(
        baseUrl.resolve('/api/cameras/$cameraId/recordings?day=$day'),
        headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
      ),
      expectedStatus: HttpStatus.ok,
      parse: (json, _) {
        if (json is! List<Object?>) return null;
        final pieces = [for (final item in json) _parsePiece(item)];
        return pieces.contains(null) ? null : pieces.nonNulls.toList();
      },
    );
  }

  @override
  Future<ApiResult<SegmentPeople>> segmentPeople(
    Uri baseUrl,
    String credential,
    String segmentPath,
  ) => _sendJson(
    () => _client.get(
      baseUrl.resolve('$segmentPath/people'),
      headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
    ),
    expectedStatus: HttpStatus.ok,
    parse: (json, _) {
      if (json case {'seconds': final List<Object?> rawSeconds}) {
        final seconds = [
          for (final item in rawSeconds)
            if (item case {
              'at': final num at,
              'people': final List<Object?> rawPeople,
            })
              PeopleSecond(
                at: at.toDouble(),
                people: [
                  for (final box in rawPeople)
                    if (box case {
                      'x': final num x,
                      'y': final num y,
                      'width': final num width,
                      'height': final num height,
                    })
                      PersonBox(
                        x: x.toDouble(),
                        y: y.toDouble(),
                        width: width.toDouble(),
                        height: height.toDouble(),
                      ),
                ],
              ),
        ];
        return seconds.length == rawSeconds.length
            ? SegmentPeople(seconds: seconds)
            : null;
      }
      return null;
    },
  );

  @override
  Future<ApiResult<MotionInfo>> motion(
    Uri baseUrl,
    String credential,
    String cameraId,
    DateTime utcDay,
  ) {
    final day = _dayParameter(utcDay);
    return _sendJson(
      () => _client.get(
        baseUrl.resolve('/api/cameras/$cameraId/motion?day=$day'),
        headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
      ),
      expectedStatus: HttpStatus.ok,
      parse: (json, _) {
        if (json case {
          'sensitivity': final String name,
          'events': final List<Object?> rawEvents,
        }) {
          final sensitivity = MotionSensitivity.values.asNameMap()[name];
          final events = [
            for (final item in rawEvents)
              if (item case {
                'start': final Object? start,
                'end': final Object? end,
              })
                if ((_parseTime(start), _parseTime(end)) case (
                  final DateTime from,
                  final DateTime to,
                ))
                  MotionEventInfo(
                    start: from,
                    end: to,
                    person: item['person'] is bool
                        ? item['person'] as bool
                        : null,
                  ),
          ];
          if (sensitivity == null || events.length != rawEvents.length) {
            return null;
          }
          return MotionInfo(sensitivity: sensitivity, events: events);
        }
        return null;
      },
    );
  }

  @override
  Future<ApiFailureKind?> setMotionSensitivity(
    Uri baseUrl,
    String credential,
    String cameraId,
    MotionSensitivity sensitivity,
  ) async {
    final http.Response response;
    try {
      response = await _client
          .put(
            baseUrl.resolve('/api/cameras/$cameraId/motion-sensitivity'),
            headers: {
              HttpHeaders.contentTypeHeader: 'application/json',
              HttpHeaders.authorizationHeader: 'Bearer $credential',
            },
            body: jsonEncode({'sensitivity': sensitivity.name}),
          )
          .timeout(timeout);
    } on IOException {
      return ApiFailureKind.unreachable;
    } on http.ClientException {
      return ApiFailureKind.unreachable;
    } on TimeoutException {
      return ApiFailureKind.unreachable;
    }
    return switch (response.statusCode) {
      HttpStatus.noContent => null,
      HttpStatus.unauthorized => ApiFailureKind.unauthorized,
      HttpStatus.badRequest => ApiFailureKind.rejected,
      _ => ApiFailureKind.unexpected,
    };
  }

  static String _dayParameter(DateTime utcDay) =>
      '${utcDay.year.toString().padLeft(4, '0')}-'
      '${utcDay.month.toString().padLeft(2, '0')}-'
      '${utcDay.day.toString().padLeft(2, '0')}';

  static RecordingPieceInfo? _parsePiece(Object? json) {
    if (json is! Map<String, Object?>) return null;
    final start = _parseTime(json['start']);
    final end = _parseTime(json['end']);
    final rawSegments = json['segments'];
    if (start == null || end == null || rawSegments is! List<Object?>) {
      return null;
    }
    final segments = [
      for (final segment in rawSegments)
        if (segment case {
          'start': final Object? segmentStart,
          'end': final Object? segmentEnd,
          'url': final String url,
        })
          if ((_parseTime(segmentStart), _parseTime(segmentEnd)) case (
            final DateTime from,
            final DateTime to,
          ))
            RecordingSegmentInfo(start: from, end: to, url: url),
    ];
    if (segments.length != rawSegments.length) return null;
    return RecordingPieceInfo(start: start, end: end, segments: segments);
  }

  static DateTime? _parseTime(Object? value) =>
      value is String ? DateTime.tryParse(value)?.toUtc() : null;

  @override
  Future<ApiFailureKind?> setRecordingQuota(
    Uri baseUrl,
    String credential,
    int megabytes,
  ) async {
    final http.Response response;
    try {
      response = await _client
          .put(
            baseUrl.resolve('/api/recordings/quota'),
            headers: {
              HttpHeaders.contentTypeHeader: 'application/json',
              HttpHeaders.authorizationHeader: 'Bearer $credential',
            },
            body: jsonEncode({'quotaMb': megabytes}),
          )
          .timeout(timeout);
    } on IOException {
      return ApiFailureKind.unreachable;
    } on http.ClientException {
      return ApiFailureKind.unreachable;
    } on TimeoutException {
      return ApiFailureKind.unreachable;
    }
    return switch (response.statusCode) {
      HttpStatus.noContent => null,
      HttpStatus.unauthorized => ApiFailureKind.unauthorized,
      HttpStatus.badRequest => ApiFailureKind.rejected,
      _ => ApiFailureKind.unexpected,
    };
  }

  @override
  Future<ApiResult<List<CameraInfo>>> cameras(Uri baseUrl, String credential) {
    return _sendJson(
      () => _client.get(
        baseUrl.resolve('/api/cameras'),
        headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
      ),
      expectedStatus: HttpStatus.ok,
      parse: (json, _) {
        if (json is! List<Object?>) return null;
        final cameras = [for (final item in json) CameraInfo.tryParse(item)];
        return cameras.contains(null) ? null : cameras.nonNulls.toList();
      },
    );
  }

  Future<ApiResult<T>> _send<T>(
    Future<http.Response> Function() request, {
    required int expectedStatus,
    required T? Function(Map<String, Object?> json, Map<String, String> headers)
    parse,
  }) => _sendJson(
    request,
    expectedStatus: expectedStatus,
    parse: (json, headers) =>
        json is Map<String, Object?> ? parse(json, headers) : null,
  );

  Future<ApiResult<T>> _sendJson<T>(
    Future<http.Response> Function() request, {
    required int expectedStatus,
    required T? Function(Object? json, Map<String, String> headers) parse,
  }) async {
    final http.Response response;
    try {
      response = await request().timeout(timeout);
    } on IOException {
      return ApiFailure(ApiFailureKind.unreachable);
    } on http.ClientException {
      return ApiFailure(ApiFailureKind.unreachable);
    } on TimeoutException {
      return ApiFailure(ApiFailureKind.unreachable);
    }

    if (response.statusCode == expectedStatus) {
      final Object? json;
      try {
        json = jsonDecode(utf8.decode(response.bodyBytes));
      } on FormatException {
        return ApiFailure(ApiFailureKind.unexpected);
      }
      final value = parse(json, response.headers);
      return value == null
          ? ApiFailure(ApiFailureKind.unexpected)
          : ApiSuccess(value);
    }
    return ApiFailure(switch (response.statusCode) {
      HttpStatus.unauthorized => ApiFailureKind.unauthorized,
      HttpStatus.badRequest => ApiFailureKind.rejected,
      HttpStatus.conflict => ApiFailureKind.conflict,
      _ => ApiFailureKind.unexpected,
    });
  }
}
