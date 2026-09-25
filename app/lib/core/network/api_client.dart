import '../pairing/device_role.dart';

abstract interface class ApiClient {
  Future<bool> health(Uri baseUrl);

  Future<ApiResult<PairResult>> pair(
    Uri baseUrl, {
    required String token,
    required String name,
  });

  Future<ApiResult<MeResult>> me(Uri baseUrl, String credential);
}

enum ApiFailureKind { unreachable, unauthorized, rejected, unexpected }

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
