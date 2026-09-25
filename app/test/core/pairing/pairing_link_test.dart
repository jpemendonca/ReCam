import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/pairing/pairing_link.dart';

import '../../support/fakes.dart';

void main() {
  group('pairingLinkTarget', () {
    test('withCameraRole_targetsCameraTab', () {
      // arrange
      final link = pairingQr(role: 'camera');

      // act
      final target = pairingLinkTarget(link);

      // assert
      expect(target, PairingLinkTarget.camera);
    });

    test('withoutRole_targetsViewerTab', () {
      // arrange
      final link = pairingQr();

      // act
      final target = pairingLinkTarget(link);

      // assert
      expect(target, PairingLinkTarget.viewer);
    });

    test('withOtherLink_returnsNull', () {
      // arrange
      const link = 'https://example.com/pair';

      // act
      final target = pairingLinkTarget(link);

      // assert
      expect(target, isNull);
    });
  });
}
