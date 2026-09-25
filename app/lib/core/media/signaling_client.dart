import 'dart:async';
import 'dart:io';

import 'package:http/http.dart' as http;

class SignalingAnswer {
  const SignalingAnswer({required this.sdp, this.resource});

  final String sdp;

  /// Where to send DELETE when the session ends.
  final Uri? resource;
}

/// WHIP and WHEP share one exchange: POST the SDP offer, get the SDP answer back
/// (RFC 9725). Candidates are gathered before the offer, so no trickle PATCH is needed.
class SignalingClient {
  SignalingClient(this._client, {this.timeout = const Duration(seconds: 10)});

  final http.Client _client;
  final Duration timeout;

  /// Returns null when the server is unreachable or refuses the offer.
  Future<SignalingAnswer?> offer(
    Uri endpoint,
    String credential,
    String sdp,
  ) async {
    final http.Response response;
    try {
      response = await _client
          .post(
            endpoint,
            headers: {
              HttpHeaders.contentTypeHeader: 'application/sdp',
              HttpHeaders.authorizationHeader: 'Bearer $credential',
            },
            body: sdp,
          )
          .timeout(timeout);
    } on IOException {
      return null;
    } on http.ClientException {
      return null;
    } on TimeoutException {
      return null;
    }
    if (response.statusCode != HttpStatus.created) return null;
    final location = response.headers[HttpHeaders.locationHeader];
    return SignalingAnswer(
      sdp: response.body,
      resource: location == null ? null : endpoint.resolve(location),
    );
  }

  /// Ends the session. Best effort: the media server also drops idle sessions.
  Future<void> end(Uri resource, String credential) async {
    try {
      await _client
          .delete(
            resource,
            headers: {HttpHeaders.authorizationHeader: 'Bearer $credential'},
          )
          .timeout(timeout);
    } on IOException {
      return;
    } on http.ClientException {
      return;
    } on TimeoutException {
      return;
    }
  }
}
