import 'package:flutter/foundation.dart';

import '../core/network/api_client.dart';
import '../core/storage/credential_store.dart';

sealed class DevicesState {}

final class DevicesLoading extends DevicesState {}

final class DevicesFailed extends DevicesState {}

final class DevicesLoaded extends DevicesState {
  DevicesLoaded(this.devices);

  final List<DeviceInfo> devices;

  List<DeviceInfo> get cameras => [
    for (final device in devices)
      if (device.isCamera) device,
  ];

  List<DeviceInfo> get monitors => [
    for (final device in devices)
      if (!device.isCamera) device,
  ];
}

/// The "Devices" list: every camera and Monitor on the server, and removing them.
class DevicesController extends ChangeNotifier {
  DevicesController({required this._api, required this._session});

  final ApiClient _api;
  final PairedSession _session;
  DevicesState _state = DevicesLoading();

  DevicesState get state => _state;

  /// This phone leaves with "Reset app", not from here.
  bool isThisPhone(DeviceInfo device) =>
      device.id.toLowerCase() == _session.deviceId.toLowerCase();

  Future<void> load() async {
    final result = await _api.devices(_session.serverUrl, _session.credential);
    _setState(switch (result) {
      ApiSuccess(:final value) => DevicesLoaded(value),
      ApiFailure() => DevicesFailed(),
    });
  }

  /// Removes a device and reloads the list. False when the server refused.
  Future<bool> remove(DeviceInfo device) async {
    if (isThisPhone(device)) return false;
    final failure = await _api.removeDevice(
      _session.serverUrl,
      _session.credential,
      device.id,
    );
    await load();
    return failure == null;
  }

  void _setState(DevicesState state) {
    _state = state;
    notifyListeners();
  }
}
