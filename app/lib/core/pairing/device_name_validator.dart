enum DeviceNameError { empty, tooLong, controlCharacters }

/// Mirrors the server rules for a device name: 1..40 characters, no control characters.
class DeviceNameValidator {
  const DeviceNameValidator._();

  static const int maxLength = 40;

  /// Returns every rule the name breaks; an empty list means the name is valid.
  static List<DeviceNameError> validate(String name) {
    final trimmed = name.trim();
    return [
      if (trimmed.isEmpty) DeviceNameError.empty,
      if (trimmed.runes.length > maxLength) DeviceNameError.tooLong,
      if (trimmed.runes.any(_isControl)) DeviceNameError.controlCharacters,
    ];
  }

  static bool _isControl(int rune) =>
      rune < 0x20 || (rune >= 0x7f && rune <= 0x9f);
}
