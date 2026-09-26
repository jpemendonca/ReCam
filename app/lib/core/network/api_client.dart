import '../pairing/device_role.dart';

abstract interface class ApiClient {
  Future<bool> health(Uri baseUrl);

  Future<ApiResult<PairResult>> pair(
    Uri baseUrl, {
    required String token,
    required String name,
    required Set<DeviceRole> expectedRoles,
  });

  Future<ApiResult<MeResult>> me(Uri baseUrl, String credential);

  /// Takes this device off the server. False when the server could not be told.
  Future<bool> leave(Uri baseUrl, String credential);

  /// Creates a QR code that pairs another phone as [role] (camera or viewer).
  Future<ApiResult<PairingTokenResult>> createPairingToken(
    Uri baseUrl,
    String credential,
    DeviceRole role,
  );

  /// Whether another phone already paired with a token this phone created.
  Future<ApiResult<bool>> pairingTokenUsed(
    Uri baseUrl,
    String credential,
    String tokenId,
  );

  Future<ApiResult<List<CameraInfo>>> cameras(Uri baseUrl, String credential);

  Future<ApiResult<RecordingQuota>> recordingQuota(
    Uri baseUrl,
    String credential,
  );

  /// Every device on the server, cameras first.
  Future<ApiResult<List<DeviceInfo>>> devices(Uri baseUrl, String credential);

  /// Takes another device off the server. Returns null when it was removed.
  Future<ApiFailureKind?> removeDevice(
    Uri baseUrl,
    String credential,
    String deviceId,
  );

  /// The UTC days that have recordings of a camera, newest first.
  Future<ApiResult<List<DateTime>>> recordingDays(
    Uri baseUrl,
    String credential,
    String cameraId,
  );

  /// The stretches recorded on one UTC day.
  Future<ApiResult<List<RecordingPieceInfo>>> recordings(
    Uri baseUrl,
    String credential,
    String cameraId,
    DateTime utcDay,
  );

  /// The motion events of one UTC day, with the camera's sensitivity.
  Future<ApiResult<MotionInfo>> motion(
    Uri baseUrl,
    String credential,
    String cameraId,
    DateTime utcDay,
  );

  /// Returns null when the server saved the camera's new sensitivity.
  Future<ApiFailureKind?> setMotionSensitivity(
    Uri baseUrl,
    String credential,
    String cameraId,
    MotionSensitivity sensitivity,
  );

  /// Lets a browser in as a Monitor ("Connect browser"). Returns null when the server
  /// approved; [ApiFailureKind.rejected] when the code expired, was used or is wrong.
  Future<ApiFailureKind?> approveBrowserLink(
    Uri baseUrl,
    String credential,
    String linkId,
    String secret,
  );

  /// Returns null when the server accepted the new quota.
  Future<ApiFailureKind?> setRecordingQuota(
    Uri baseUrl,
    String credential,
    int megabytes,
  );
}

/// A device in the Monitor's device list.
class DeviceInfo {
  const DeviceInfo({
    required this.id,
    required this.name,
    required this.role,
    required this.online,
  });

  final String id;
  final String name;
  final DeviceRole role;
  final bool online;

  /// Owners and viewers are both Monitors on screen.
  bool get isCamera => role == DeviceRole.camera;
}

/// One playable recorded file; [url] is a server path. Times in UTC.
class RecordingSegmentInfo {
  const RecordingSegmentInfo({
    required this.start,
    required this.end,
    required this.url,
  });

  final DateTime start;
  final DateTime end;
  final String url;
}

/// A continuous stretch of recording, made of back-to-back segments. Times in UTC.
class RecordingPieceInfo {
  const RecordingPieceInfo({
    required this.start,
    required this.end,
    required this.segments,
  });

  final DateTime start;
  final DateTime end;
  final List<RecordingSegmentInfo> segments;
}

/// How little movement counts as motion. High finds the smallest movements.
enum MotionSensitivity { low, medium, high }

/// Something moved in the recording between [start] and [end]. Times in UTC.
class MotionEventInfo {
  const MotionEventInfo({required this.start, required this.end});

  final DateTime start;
  final DateTime end;
}

/// A camera's motion in one UTC day, and the sensitivity that found it.
class MotionInfo {
  const MotionInfo({required this.sensitivity, required this.events});

  final MotionSensitivity sensitivity;
  final List<MotionEventInfo> events;
}

/// The space all recordings may take together, and the disk around it.
class RecordingQuota {
  const RecordingQuota({
    required this.megabytes,
    required this.usedBytes,
    required this.freeBytes,
  });

  static const bytesPerMegabyte = 1024 * 1024;

  final int megabytes;
  final int usedBytes;
  final int freeBytes;

  /// The largest quota the disk allows: what recordings use plus what is still free.
  int get maxMegabytes => (usedBytes + freeBytes) ~/ bytesPerMegabyte;
}

enum ApiFailureKind {
  unreachable,
  unauthorized,
  rejected,
  conflict,
  unexpected,
}

sealed class ApiResult<T> {}

final class ApiSuccess<T> extends ApiResult<T> {
  ApiSuccess(this.value);

  final T value;
}

final class ApiFailure<T> extends ApiResult<T> {
  ApiFailure(this.kind);

  final ApiFailureKind kind;
}

class PairResult {
  const PairResult({
    required this.deviceId,
    required this.credential,
    required this.role,
    required this.serverName,
  });

  final String deviceId;
  final String credential;
  final DeviceRole role;
  final String serverName;
}

class MeResult {
  const MeResult({
    required this.deviceId,
    required this.name,
    required this.role,
  });

  final String deviceId;
  final String name;
  final DeviceRole role;
}

class PairingTokenResult {
  const PairingTokenResult({
    required this.id,
    required this.qrUri,
    required this.validFor,
  });

  /// Asks [ApiClient.pairingTokenUsed] about this token.
  final String id;
  final String qrUri;

  /// How long the token stays valid, counted from the moment the server answered.
  final Duration validFor;
}

/// A camera as viewers see it: stored telemetry plus live presence. The same shape comes
/// from `GET /api/cameras` and the hub message `CameraStatusChanged`.
class CameraInfo {
  const CameraInfo({
    required this.id,
    required this.name,
    required this.online,
    required this.publishing,
    this.batteryLevel,
    this.isCharging,
    this.temperatureC,
    this.recording = false,
    this.canRecord = true,
  });

  final String id;
  final String name;
  final bool online;
  final bool publishing;
  final int? batteryLevel;
  final bool? isCharging;
  final double? temperatureC;

  /// "Record always" is on for this camera.
  final bool recording;

  /// False for a camera that sends VP8, which the server cannot record.
  final bool canRecord;

  /// Returns null for data that does not describe a camera.
  static CameraInfo? tryParse(Object? json) {
    if (json is! Map<String, Object?>) return null;
    final id = json['id'];
    final name = json['name'];
    final online = json['online'];
    final publishing = json['publishing'];
    final batteryLevel = json['batteryLevel'];
    final isCharging = json['isCharging'];
    final temperatureC = json['temperatureC'];
    final recording = json['recording'];
    final canRecord = json['canRecord'];
    if (id is! String || name is! String || online is! bool) return null;
    return CameraInfo(
      id: id,
      name: name,
      online: online,
      publishing: publishing == true,
      batteryLevel: batteryLevel is num ? batteryLevel.toInt() : null,
      isCharging: isCharging is bool ? isCharging : null,
      temperatureC: temperatureC is num ? temperatureC.toDouble() : null,
      recording: recording == true,
      canRecord: canRecord != false,
    );
  }
}
