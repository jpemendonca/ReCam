import 'package:flutter/services.dart';

/// Whether the phone has any network at all (Wi-Fi, mobile data, cable), as Android knows it.
/// Nothing is sent anywhere to find out.
abstract interface class NetworkStatus {
  Future<bool> hasNetwork();
}

class PlatformNetworkStatus implements NetworkStatus {
  static const _device = MethodChannel('io.recam.app/device');

  // A phone that cannot answer counts as connected: the connection attempt says the rest.
  @override
  Future<bool> hasNetwork() async {
    try {
      return await _device.invokeMethod<bool>('hasNetwork') ?? true;
    } on PlatformException {
      return true;
    } on MissingPluginException {
      return true;
    }
  }
}
