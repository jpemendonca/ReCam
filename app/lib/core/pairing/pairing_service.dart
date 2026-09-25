import '../network/api_client.dart';
import '../network/pinned_http_overrides.dart';
import '../storage/credential_store.dart';
import 'device_role.dart';
import 'qr_payload.dart';

enum PairingFailure {
  invalidQr,
  serverUnreachable,
  tokenRejected,
  invalidInput,
  wrongRole,
  unexpected,

  /// A saved pairing stopped working: the server was reinstalled or forgot this phone.
  pairingLost,
}

sealed class PairingOutcome {}

final class PairingSucceeded extends PairingOutcome {
  PairingSucceeded(this.session);

  final PairedSession session;
}

final class PairingFailed extends PairingOutcome {
  PairingFailed(this.reason);

  final PairingFailure reason;
}

/// What the server says about a saved pairing. [revoked] and [serverChanged] are final: the
/// pairing is forgotten. [unknown] means the server could not be asked.
enum SessionStatus { valid, revoked, serverChanged, unknown }

/// Pairs this phone with a server from a scanned QR code and keeps the result per tab.
class PairingService {
  PairingService({
    required this._api,
    required this._store,
    required this._pins,
  });

  final ApiClient _api;
  final CredentialStore _store;
  final PinnedHttpOverrides _pins;

  Future<PairingOutcome> pairFromQr({
    required String rawQr,
    required String deviceName,
    required PairingSlot slot,
    required Set<DeviceRole> acceptedRoles,
  }) async {
    final parsed = QrPayload.parse(rawQr);
    if (parsed is! QrParseOk) return PairingFailed(PairingFailure.invalidQr);
    final payload = parsed.payload;

    final fingerprint = payload.fingerprint;
    if (fingerprint != null) {
      for (final url in payload.serverUrls) {
        _pins.pin(url, fingerprint);
      }
    }

    final serverUrl = await _firstReachable(payload.serverUrls);
    if (serverUrl == null) {
      return PairingFailed(PairingFailure.serverUnreachable);
    }

    final result = await _api.pair(
      serverUrl,
      token: payload.token,
      name: deviceName,
      expectedRoles: acceptedRoles,
    );
    switch (result) {
      case ApiFailure(:final kind):
        return PairingFailed(switch (kind) {
          ApiFailureKind.unreachable => PairingFailure.serverUnreachable,
          ApiFailureKind.unauthorized => PairingFailure.tokenRejected,
          ApiFailureKind.rejected => PairingFailure.invalidInput,
          ApiFailureKind.conflict => PairingFailure.wrongRole,
          ApiFailureKind.unexpected => PairingFailure.unexpected,
        });
      case ApiSuccess(:final value):
        if (!acceptedRoles.contains(value.role)) {
          return PairingFailed(PairingFailure.wrongRole);
        }
        final session = PairedSession(
          serverUrl: serverUrl,
          fingerprint: fingerprint,
          credential: value.credential,
          deviceId: value.deviceId,
          role: value.role,
          serverName: value.serverName,
        );
        await _store.write(slot, session);
        return PairingSucceeded(session);
    }
  }

  /// Reads the saved pairing and pins its certificate again, so the app trusts the server
  /// after a restart. It makes no network call.
  Future<PairedSession?> restore(PairingSlot slot) async {
    final session = await _store.read(slot);
    if (session == null) return null;
    final fingerprint = session.fingerprint;
    if (fingerprint != null) _pins.pin(session.serverUrl, fingerprint);
    return session;
  }

  /// Asks the server whether the credential still works. A revoked device, or a server whose
  /// certificate no longer matches the pinned one, is forgotten.
  Future<SessionStatus> verify(PairingSlot slot, PairedSession session) async {
    final result = await _api.me(session.serverUrl, session.credential);
    switch (result) {
      case ApiSuccess():
        return SessionStatus.valid;
      case ApiFailure(kind: ApiFailureKind.unauthorized):
        await forget(slot);
        return SessionStatus.revoked;
      case ApiFailure(kind: ApiFailureKind.unreachable)
          when _pins.certificateChanged(session.serverUrl):
        await forget(slot);
        return SessionStatus.serverChanged;
      case ApiFailure():
        return SessionStatus.unknown;
    }
  }

  /// Deletes the saved pairing of one tab. The server keeps the device until it is revoked.
  Future<void> forget(PairingSlot slot) => _store.delete(slot);

  Future<Uri?> _firstReachable(List<Uri> urls) async {
    for (final url in urls) {
      if (await _api.health(url)) return url;
    }
    return null;
  }
}
