import 'dart:async';
import 'dart:io' show X509Certificate;
import 'dart:typed_data';

import 'package:flutter/widgets.dart' show Key, SizedBox, Widget;

import 'package:recam/core/media/image_adjustment.dart';
import 'package:recam/core/storage/adjustment_store.dart';
import 'package:recam/core/device/battery_optimization.dart';
import 'package:recam/core/device/battery_reader.dart';
import 'package:recam/core/device/keep_alive.dart';
import 'package:recam/core/device/screen_controller.dart';
import 'package:recam/core/media/camera_capture.dart';
import 'package:recam/core/media/recording_player.dart';
import 'package:recam/core/media/recording_relay.dart';
import 'package:recam/core/media/video_quality.dart';
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

  bool leaveResult = true;
  final List<String> leaveCalls = [];

  @override
  Future<bool> leave(Uri baseUrl, String credential) async {
    leaveCalls.add(credential);
    return leaveResult;
  }

  ApiResult<RecordingQuota> quotaResult = ApiFailure(
    ApiFailureKind.unreachable,
  );
  ApiFailureKind? setQuotaFailure;
  final List<int> quotaChanges = [];

  @override
  Future<ApiResult<RecordingQuota>> recordingQuota(
    Uri baseUrl,
    String credential,
  ) async => quotaResult;

  @override
  Future<ApiFailureKind?> setRecordingQuota(
    Uri baseUrl,
    String credential,
    int megabytes,
  ) async {
    quotaChanges.add(megabytes);
    return setQuotaFailure;
  }

  final List<ApiResult<List<DeviceInfo>>> deviceResults = [];
  ApiFailureKind? removeFailure;
  final List<String> removedDevices = [];

  @override
  Future<ApiResult<List<DeviceInfo>>> devices(
    Uri baseUrl,
    String credential,
  ) async => deviceResults.isEmpty
      ? ApiSuccess(const <DeviceInfo>[])
      : deviceResults.removeAt(0);

  @override
  Future<ApiFailureKind?> removeDevice(
    Uri baseUrl,
    String credential,
    String deviceId,
  ) async {
    removedDevices.add(deviceId);
    return removeFailure;
  }

  final List<({String linkId, String secret})> approvedLinks = [];
  ApiFailureKind? approveLinkFailure;

  @override
  Future<ApiFailureKind?> approveBrowserLink(
    Uri baseUrl,
    String credential,
    String linkId,
    String secret,
  ) async {
    approvedLinks.add((linkId: linkId, secret: secret));
    return approveLinkFailure;
  }

  List<DateTime> recordingDaysResult = [];
  final Map<DateTime, List<RecordingPieceInfo>> recordingsByDay = {};
  final List<DateTime> recordingDayCalls = [];

  @override
  Future<ApiResult<List<DateTime>>> recordingDays(
    Uri baseUrl,
    String credential,
    String cameraId,
  ) async => ApiSuccess(recordingDaysResult);

  @override
  Future<ApiResult<List<RecordingPieceInfo>>> recordings(
    Uri baseUrl,
    String credential,
    String cameraId,
    DateTime utcDay,
  ) async {
    recordingDayCalls.add(utcDay);
    return ApiSuccess(recordingsByDay[utcDay] ?? const []);
  }

  MotionSensitivity motionSensitivity = MotionSensitivity.medium;
  final Map<DateTime, List<MotionEventInfo>> motionByDay = {};

  /// What the motion answers after the sensitivity changes, by UTC day.
  final Map<DateTime, List<MotionEventInfo>> motionAfterChange = {};
  ApiFailureKind? sensitivityFailure;

  @override
  Future<ApiResult<MotionInfo>> motion(
    Uri baseUrl,
    String credential,
    String cameraId,
    DateTime utcDay,
  ) async => ApiSuccess(
    MotionInfo(
      sensitivity: motionSensitivity,
      events: motionByDay[utcDay] ?? const [],
    ),
  );

  @override
  Future<ApiFailureKind?> setMotionSensitivity(
    Uri baseUrl,
    String credential,
    String cameraId,
    MotionSensitivity sensitivity,
  ) async {
    if (sensitivityFailure != null) return sensitivityFailure;
    motionSensitivity = sensitivity;
    motionByDay.addAll(motionAfterChange);
    return null;
  }

  final List<DeviceRole> tokenRoles = [];

  /// Answers for the next "was it used" questions; when empty, not used yet.
  final List<ApiResult<bool>> tokenUsedResults = [];
  final List<String> tokenUsedCalls = [];

  @override
  Future<ApiResult<bool>> pairingTokenUsed(
    Uri baseUrl,
    String credential,
    String tokenId,
  ) async {
    tokenUsedCalls.add(tokenId);
    return tokenUsedResults.isEmpty
        ? ApiSuccess(false)
        : tokenUsedResults.removeAt(0);
  }

  int get tokenCalls => tokenRoles.length;

  @override
  Future<ApiResult<PairingTokenResult>> createPairingToken(
    Uri baseUrl,
    String credential,
    DeviceRole role,
  ) async {
    tokenRoles.add(role);
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
  String? role = 'viewer',
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

class FakeCertificate implements X509Certificate {
  FakeCertificate(List<int> der) : der = Uint8List.fromList(der);

  @override
  final Uint8List der;

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

class FakeLinkSource implements LinkSource {
  final controller = StreamController<Uri>.broadcast();

  @override
  Stream<Uri> get links => controller.stream;
}

/// Brighten settings in memory.
class FakeAdjustmentStore implements AdjustmentStore {
  final Map<String, ImageAdjustment> saved = {};

  @override
  Future<ImageAdjustment> read(String cameraId) async =>
      saved[cameraId] ?? ImageAdjustment.normal;

  @override
  Future<void> write(String cameraId, ImageAdjustment adjustment) async {
    if (adjustment.isNormal) {
      saved.remove(cameraId);
    } else {
      saved[cameraId] = adjustment;
    }
  }
}
