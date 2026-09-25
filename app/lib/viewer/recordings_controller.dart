import 'package:flutter/foundation.dart';

import '../core/network/api_client.dart';
import '../core/storage/credential_store.dart';

sealed class RecordingsState {}

final class RecordingsLoading extends RecordingsState {}

final class RecordingsFailed extends RecordingsState {}

final class RecordingsReady extends RecordingsState {
  RecordingsReady({required this.quota, required this.selectedMegabytes});

  final RecordingQuota quota;

  /// Where the slider is; saved only on [RecordingsController.save].
  final int selectedMegabytes;
}

/// The "Recordings" slider: how much disk all recordings may take.
class RecordingsController extends ChangeNotifier {
  RecordingsController({required this._api, required this._session});

  static const minimumMegabytes = 100;

  /// A camera sends at most 700 kbps: about 300 MB per hour of recording.
  static const megabytesPerCameraHour = 300;

  final ApiClient _api;
  final PairedSession _session;
  RecordingsState _state = RecordingsLoading();
  bool _saving = false;

  RecordingsState get state => _state;

  bool get saving => _saving;

  /// How many hours of one camera fit in [megabytes].
  static double hoursFor(int megabytes) => megabytes / megabytesPerCameraHour;

  Future<void> load() async {
    _setState(RecordingsLoading());
    final result = await _api.recordingQuota(
      _session.serverUrl,
      _session.credential,
    );
    _setState(switch (result) {
      ApiSuccess(:final value) => RecordingsReady(
        quota: value,
        selectedMegabytes: value.megabytes,
      ),
      ApiFailure() => RecordingsFailed(),
    });
  }

  /// Moves the slider, kept between the minimum and what the disk allows.
  void select(int megabytes) {
    final state = _state;
    if (state is! RecordingsReady) return;
    final upper = state.quota.maxMegabytes < minimumMegabytes
        ? minimumMegabytes
        : state.quota.maxMegabytes;
    _setState(
      RecordingsReady(
        quota: state.quota,
        selectedMegabytes: megabytes.clamp(minimumMegabytes, upper),
      ),
    );
  }

  /// Stores the selected quota. False when the server refuses or cannot be reached.
  Future<bool> save() async {
    final state = _state;
    if (state is! RecordingsReady || _saving) return false;
    _saving = true;
    notifyListeners();
    final failure = await _api.setRecordingQuota(
      _session.serverUrl,
      _session.credential,
      state.selectedMegabytes,
    );
    _saving = false;
    if (failure == null) {
      _setState(
        RecordingsReady(
          quota: RecordingQuota(
            megabytes: state.selectedMegabytes,
            usedBytes: state.quota.usedBytes,
            freeBytes: state.quota.freeBytes,
          ),
          selectedMegabytes: state.selectedMegabytes,
        ),
      );
      return true;
    }
    notifyListeners();
    return false;
  }

  void _setState(RecordingsState state) {
    _state = state;
    notifyListeners();
  }
}
