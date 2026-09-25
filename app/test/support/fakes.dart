import 'dart:async';

import 'package:flutter/widgets.dart' show SizedBox, Widget;

import 'package:recam/core/device/battery_reader.dart';
import 'package:recam/core/device/keep_alive.dart';
import 'package:recam/core/device/screen_controller.dart';
import 'package:recam/core/media/webrtc_publisher.dart';
import 'package:recam/core/media/webrtc_viewer.dart';
import 'package:recam/core/network/api_client.dart';
import 'package:recam/core/network/hub_client.dart';
import 'package:recam/core/pairing/device_role.dart';
import 'package:recam/core/pairing/pairing_link.dart';
import 'package:recam/core/storage/credential_store.dart';

part 'device_fakes.dart';

class FakeApiClient implements ApiClient {
  final List<ApiResult<List<CameraInfo>>> cameraResults = [];
  int cameraCalls = 0;

  @override
  Future<ApiResult<List<CameraInfo>>> cameras(
    Uri baseUrl,
    String credential,
  ) async {
    cameraCalls++;
    return cameraResults.isEmpty
        ? ApiSuccess(const <CameraInfo>[])
        : cameraResults.removeAt(0);
  }

  Set<String> healthyHosts = {};
  ApiResult<PairResult> pairResult = ApiFailure(ApiFailureKind.unexpected);
  ApiResult<MeResult> meResult = ApiFailure(ApiFailureKind.unreachable);
  final List<ApiResult<PairingTokenResult>> tokenResults = [];

  final List<Uri> healthCalls = [];
  final List<
    ({Uri baseUrl, String token, String name, Set<DeviceRole> expectedRoles})
  >
  pairCalls = [];

  @override
  Future<bool> health(Uri baseUrl) async {
    healthCalls.add(baseUrl);
    return healthyHosts.contains(baseUrl.host);
  }

  @override
  Future<ApiResult<PairResult>> pair(
    Uri baseUrl, {
    required String token,
    required String name,
    required Set<DeviceRole> expectedRoles,
  }) async {
    pairCalls.add((
      baseUrl: baseUrl,
      token: token,
      name: name,
      expectedRoles: expectedRoles,
    ));
    return pairResult;
  }

  @override
  Future<ApiResult<MeResult>> me(Uri baseUrl, String credential) async =>
      meResult;

  int tokenCalls = 0;

  @override
  Future<ApiResult<PairingTokenResult>> createCameraPairingToken(
    Uri baseUrl,
    String credential,
  ) async {
    tokenCalls++;
    return tokenResults.removeAt(0);
  }
}

class MemoryCredentialStore implements CredentialStore {
  final Map<PairingSlot, PairedSession> sessions = {};

  @override
  Future<PairedSession?> read(PairingSlot slot) async => sessions[slot];

  @override
  Future<void> write(PairingSlot slot, PairedSession session) async {
    sessions[slot] = session;
  }

  @override
  Future<void> delete(PairingSlot slot) async {
    sessions.remove(slot);
  }
}

const fingerprint =
    '039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81';

String pairingQr({
  String token = 'tok',
  String? fingerprint = fingerprint,
  List<String> urls = const ['https://192.168.0.10:8443'],
  String? role,
}) {
  final query = [
    'v=1',
    't=$token',
    if (role != null) 'r=$role',
    if (fingerprint != null) 'f=$fingerprint',
    for (final url in urls) 'u=${Uri.encodeQueryComponent(url)}',
  ].join('&');
  return 'recam://pair?$query';
}

PairResult pairResult({DeviceRole role = DeviceRole.owner}) => PairResult(
  deviceId: '0123456789abcdef0123456789abcdef',
  credential: '0123456789abcdef0123456789abcdef.secret',
  role: role,
  serverName: 'ReCam',
);

PairedSession pairedSession({DeviceRole role = DeviceRole.owner}) =>
    PairedSession(
      serverUrl: Uri.parse('https://192.168.0.10:8443'),
      fingerprint: fingerprint,
      credential: '0123456789abcdef0123456789abcdef.secret',
      deviceId: '0123456789abcdef0123456789abcdef',
      role: role,
      serverName: 'ReCam',
    );

class FakeLinkSource implements LinkSource {
  final controller = StreamController<Uri>.broadcast();

  @override
  Stream<Uri> get links => controller.stream;
}
