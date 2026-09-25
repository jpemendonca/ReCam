enum DeviceRole {
  owner,
  viewer,
  camera;

  static DeviceRole? tryParse(String? value) {
    for (final role in values) {
      if (role.name == value) return role;
    }
    return null;
  }
}
