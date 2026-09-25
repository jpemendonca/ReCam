import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/pinned_http_overrides.dart';

import '../../support/fakes.dart';

void main() {
  group('PinnedHttpOverrides.accepts', () {
    final certificate = FakeCertificate([1, 2, 3]);
    final serverUrl = Uri.parse('https://192.168.0.10:8443');

    test('withMatchingFingerprint_acceptsCertificate', () {
      // arrange
      final overrides = PinnedHttpOverrides()..pin(serverUrl, fingerprint);

      // act
      final accepted = overrides.accepts(certificate, '192.168.0.10', 8443);

      // assert
      expect(accepted, isTrue);
    });

    test('withDifferentFingerprint_rejectsCertificate', () {
      // arrange
      final overrides = PinnedHttpOverrides()..pin(serverUrl, 'a' * 64);

      // act
      final accepted = overrides.accepts(certificate, '192.168.0.10', 8443);

      // assert
      expect(accepted, isFalse);
    });

    test('withUpperCasePin_stillAcceptsMatchingCertificate', () {
      // arrange
      final overrides = PinnedHttpOverrides()
        ..pin(serverUrl, fingerprint.toUpperCase());

      // act
      final accepted = overrides.accepts(certificate, '192.168.0.10', 8443);

      // assert
      expect(accepted, isTrue);
    });

    test('forHostWithoutPin_rejectsCertificate', () {
      // arrange
      final overrides = PinnedHttpOverrides()..pin(serverUrl, fingerprint);

      // act
      final accepted = overrides.accepts(certificate, '10.0.0.5', 8443);

      // assert
      expect(accepted, isFalse);
    });

    test('forSameHostOnAnotherPort_rejectsCertificate', () {
      // arrange
      final overrides = PinnedHttpOverrides()..pin(serverUrl, fingerprint);

      // act
      final accepted = overrides.accepts(certificate, '192.168.0.10', 9443);

      // assert
      expect(accepted, isFalse);
    });
  });

  group('PinnedHttpOverrides.certificateChanged', () {
    final pinnedCertificate = FakeCertificate([1, 2, 3]);
    final otherCertificate = FakeCertificate([4, 5, 6]);
    final serverUrl = Uri.parse('https://192.168.0.10:8443');

    test('afterDifferentCertificate_returnsTrue', () {
      // arrange
      final overrides = PinnedHttpOverrides()..pin(serverUrl, fingerprint);

      // act
      overrides.accepts(otherCertificate, '192.168.0.10', 8443);

      // assert
      expect(overrides.certificateChanged(serverUrl), isTrue);
    });

    test('afterPinnedCertificateAgain_returnsFalse', () {
      // arrange
      final overrides = PinnedHttpOverrides()..pin(serverUrl, fingerprint);
      overrides.accepts(otherCertificate, '192.168.0.10', 8443);

      // act
      overrides.accepts(pinnedCertificate, '192.168.0.10', 8443);

      // assert
      expect(overrides.certificateChanged(serverUrl), isFalse);
    });

    test('afterPairingAgain_returnsFalse', () {
      // arrange
      final overrides = PinnedHttpOverrides()..pin(serverUrl, 'a' * 64);
      overrides.accepts(otherCertificate, '192.168.0.10', 8443);

      // act
      overrides.pin(serverUrl, fingerprint);

      // assert
      expect(overrides.certificateChanged(serverUrl), isFalse);
    });
  });
}
