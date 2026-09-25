import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'credential_store.dart';

class SecureCredentialStore implements CredentialStore {
  SecureCredentialStore([FlutterSecureStorage? storage])
    : _storage = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _storage;

  @override
  Future<PairedSession?> read(PairingSlot slot) async {
    final source = await _storage.read(key: _key(slot));
    return source == null ? null : PairedSession.tryParse(source);
  }

  @override
  Future<void> write(PairingSlot slot, PairedSession session) =>
      _storage.write(key: _key(slot), value: session.toJsonString());

  @override
  Future<void> delete(PairingSlot slot) => _storage.delete(key: _key(slot));

  static String _key(PairingSlot slot) => 'pairing.${slot.name}';
}
