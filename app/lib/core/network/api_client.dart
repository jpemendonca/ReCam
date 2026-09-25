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
