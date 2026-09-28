import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Where "Show people" is kept, on this phone only.
abstract interface class PeopleBoxesStore {
  /// Whether the boxes around people show; on when nothing was saved.
  Future<bool> read();

  Future<void> write(bool show);
}

class SecurePeopleBoxesStore implements PeopleBoxesStore {
  SecurePeopleBoxesStore([FlutterSecureStorage? storage])
    : _storage = storage ?? const FlutterSecureStorage();

  static const _key = 'people-boxes';

  final FlutterSecureStorage _storage;

  @override
  Future<bool> read() async => await _storage.read(key: _key) != 'off';

  @override
  Future<void> write(bool show) =>
      _storage.write(key: _key, value: show ? 'on' : 'off');
}
