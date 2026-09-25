import 'dart:io';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/network/pinned_http_overrides.dart';

import '../../support/fakes.dart';

class _FakeCertificate implements X509Certificate {
  _FakeCertificate(this.der);

  @override
  final Uint8List der;

  @override
  dynamic noSuchMethod(Invocation invocation) => super.noSuchMethod(invocation);
}

void main() {
  group('PinnedHttpOverrides.accepts', () {
    final certificate = _FakeCertificate(Uint8List.fromList([1, 2, 3]));
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
}
