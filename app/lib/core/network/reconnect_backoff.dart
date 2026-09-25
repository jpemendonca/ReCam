/// Waits between reconnection attempts: 1, 2, 4, 8, 16, then 30 s forever (SPECS.md 2.3).
class ReconnectBackoff {
  static const _steps = [1, 2, 4, 8, 16, 30];

  int _attempt = 0;

  Duration next() {
    final seconds =
        _steps[_attempt < _steps.length ? _attempt : _steps.length - 1];
    _attempt++;
    return Duration(seconds: seconds);
  }

  void reset() => _attempt = 0;
}
