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
  }) {
    return _send(
      () => _client.post(
        baseUrl.resolve('/api/pair'),
        headers: {HttpHeaders.contentTypeHeader: 'application/json'},
        body: jsonEncode({'token': token, 'name': name}),
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

  @override
  Future<ApiResult<PairingTokenResult>> createCameraPairingToken(
    Uri baseUrl,
    String credential,
  ) {
    return _send(
      () => _client.post(
        baseUrl.resolve('/api/pairing-tokens'),
        headers: {
          HttpHeaders.contentTypeHeader: 'application/json',
          HttpHeaders.authorizationHeader: 'Bearer $credential',
        },
        body: jsonEncode({'role': 'camera'}),
      ),
      expectedStatus: HttpStatus.created,
      parse: (json, headers) {
        final qrUri = json['qrUri'];
        final expiresAt = DateTime.tryParse(json['expiresAt'] as String? ?? '');
        if (qrUri is! String || expiresAt == null) return null;
        return PairingTokenResult(
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

  Future<ApiResult<T>> _send<T>(
    Future<http.Response> Function() request, {
    required int expectedStatus,
    required T? Function(Map<String, Object?> json, Map<String, String> headers)
    parse,
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
      final value = json is Map<String, Object?>
          ? parse(json, response.headers)
          : null;
      return value == null
          ? ApiFailure(ApiFailureKind.unexpected)
          : ApiSuccess(value);
    }
    return ApiFailure(switch (response.statusCode) {
      HttpStatus.unauthorized => ApiFailureKind.unauthorized,
      HttpStatus.badRequest => ApiFailureKind.rejected,
      _ => ApiFailureKind.unexpected,
    });
  }
}
