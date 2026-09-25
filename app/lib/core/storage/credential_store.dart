import 'dart:convert';

import '../pairing/device_role.dart';

/// Each tab keeps its own pairing, so one phone can be a camera and a viewer.
enum PairingSlot { camera, viewer }

abstract interface class CredentialStore {
  Future<PairedSession?> read(PairingSlot slot);

  Future<void> write(PairingSlot slot, PairedSession session);

  Future<void> delete(PairingSlot slot);
}

class PairedSession {
  const PairedSession({
    required this.serverUrl,
    required this.credential,
    required this.deviceId,
    required this.role,
    required this.serverName,
    this.fingerprint,
  });

  final Uri serverUrl;
  final String? fingerprint;
  final String credential;
  final String deviceId;
  final DeviceRole role;
  final String serverName;

  String toJsonString() => jsonEncode({
    'serverUrl': serverUrl.toString(),
    'fingerprint': fingerprint,
    'credential': credential,
    'deviceId': deviceId,
    'role': role.name,
    'serverName': serverName,
  });

  /// Returns null for data that does not describe a session, so a corrupted entry reads
  /// as "not paired".
  static PairedSession? tryParse(String source) {
    final Object? json;
    try {
      json = jsonDecode(source);
    } on FormatException {
      return null;
    }
    if (json is! Map<String, Object?>) return null;

    final serverUrl = json['serverUrl'];
    final credential = json['credential'];
    final deviceId = json['deviceId'];
    final role = json['role'];
    final serverName = json['serverName'];
    final fingerprint = json['fingerprint'];
    final parsedRole = role is String ? DeviceRole.tryParse(role) : null;
    if (serverUrl is! String ||
        credential is! String ||
        deviceId is! String ||
        parsedRole == null ||
        serverName is! String ||
        (fingerprint != null && fingerprint is! String)) {
      return null;
    }
    return PairedSession(
      serverUrl: Uri.parse(serverUrl),
      fingerprint: fingerprint as String?,
      credential: credential,
      deviceId: deviceId,
      role: parsedRole,
      serverName: serverName,
    );
  }
}
