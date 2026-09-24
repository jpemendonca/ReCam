import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

final _importPattern = RegExp(
  r'''^import\s+['"]([^'"]+)['"]''',
  multiLine: true,
);

List<String> _violations(String fromDir, List<String> forbiddenDirs) {
  final violations = <String>[];
  final root = Directory('lib/$fromDir');
  if (!root.existsSync()) return violations;
  for (final file in root.listSync(recursive: true).whereType<File>()) {
    if (!file.path.endsWith('.dart')) continue;
    for (final match in _importPattern.allMatches(file.readAsStringSync())) {
      final target = _resolve(file, match.group(1)!);
      if (target == null) continue;
      for (final forbidden in forbiddenDirs) {
        if (target.startsWith('$forbidden/')) {
          violations.add('${file.path} -> ${match.group(1)}');
        }
      }
    }
  }
  return violations;
}

// Returns the import target relative to lib/, or null for external packages.
String? _resolve(File file, String uri) {
  const packagePrefix = 'package:recam/';
  if (uri.startsWith(packagePrefix)) return uri.substring(packagePrefix.length);
  if (uri.contains(':')) return null;
  final libDir = Directory('lib').absolute.uri;
  final resolved = file.absolute.uri.resolve(uri);
  return resolved.path.substring(libDir.path.length);
}

void main() {
  group('Layer imports', () {
    test('camera_doesNotImportViewer', () {
      // arrange
      const from = 'camera';

      // act
      final violations = _violations(from, ['viewer']);

      // assert
      expect(violations, isEmpty);
    });

    test('viewer_doesNotImportCamera', () {
      // arrange
      const from = 'viewer';

      // act
      final violations = _violations(from, ['camera']);

      // assert
      expect(violations, isEmpty);
    });

    test('core_doesNotImportCameraOrViewer', () {
      // arrange
      const from = 'core';

      // act
      final violations = _violations(from, ['camera', 'viewer']);

      // assert
      expect(violations, isEmpty);
    });
  });
}
