import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../media/image_adjustment.dart';

/// Where each camera's "Brighten" setting is kept, on this phone only.
abstract interface class AdjustmentStore {
  Future<ImageAdjustment> read(String cameraId);

  Future<void> write(String cameraId, ImageAdjustment adjustment);
}

class SecureAdjustmentStore implements AdjustmentStore {
  SecureAdjustmentStore([FlutterSecureStorage? storage])
    : _storage = storage ?? const FlutterSecureStorage();

  final FlutterSecureStorage _storage;

  @override
  Future<ImageAdjustment> read(String cameraId) async =>
      ImageAdjustment.decode(await _storage.read(key: _key(cameraId)));

  @override
  Future<void> write(String cameraId, ImageAdjustment adjustment) =>
      adjustment.isNormal
      ? _storage.delete(key: _key(cameraId))
      : _storage.write(key: _key(cameraId), value: adjustment.encode());

  static String _key(String cameraId) => 'brighten.${cameraId.toLowerCase()}';
}
