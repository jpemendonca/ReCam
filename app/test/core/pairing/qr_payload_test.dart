import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/pairing/qr_payload.dart';

import '../../support/fakes.dart';

void main() {
  group('QrPayload.parse', () {
    test('withValidUri_returnsPayload', () {
      // arrange
      final raw = pairingQr(
        token: 'abc-_123',
        urls: ['https://192.168.0.10:8443', 'https://10.0.0.5:8443'],
      );

      // act
      final result = QrPayload.parse(raw);

      // assert
      final payload = (result as QrParseOk).payload;
      expect(payload.token, 'abc-_123');
      expect(payload.fingerprint, fingerprint);
      expect(payload.serverUrls, [
        Uri.parse('https://192.168.0.10:8443'),
        Uri.parse('https://10.0.0.5:8443'),
      ]);
    });

    test('withoutFingerprint_returnsPayloadWithoutPin', () {
      // arrange
      final raw = pairingQr(fingerprint: null);

      // act
      final result = QrPayload.parse(raw);

      // assert
      expect((result as QrParseOk).payload.fingerprint, isNull);
    });

    test('withoutToken_returnsMissingTokenError', () {
      // arrange
      const raw = 'recam://pair?v=1&u=https%3A%2F%2F192.168.0.10%3A8443';

      // act
      final result = QrPayload.parse(raw);

      // assert
      expect((result as QrParseFailed).errors, [QrError.missingToken]);
    });

    test('withoutUrl_returnsMissingUrlError', () {
      // arrange
      const raw = 'recam://pair?v=1&t=abc';

      // act
      final result = QrPayload.parse(raw);

      // assert
      expect((result as QrParseFailed).errors, [QrError.missingUrl]);
    });

    test('withMalformedUrl_returnsMissingUrlError', () {
      // arrange
      const raw = 'recam://pair?v=1&t=abc&u=not-a-url&u=ftp%3A%2F%2Fhost';

      // act
      final result = QrPayload.parse(raw);

      // assert
      expect((result as QrParseFailed).errors, [QrError.missingUrl]);
    });

    test('withInvalidFingerprint_returnsInvalidFingerprintError', () {
      // arrange
      final raw = pairingQr(fingerprint: 'ABC123');

      // act
      final result = QrPayload.parse(raw);

      // assert
      expect((result as QrParseFailed).errors, [QrError.invalidFingerprint]);
    });

    test('withUnsupportedVersion_returnsUnsupportedVersionError', () {
      // arrange
      const raw = 'recam://pair?v=2&t=abc&u=https%3A%2F%2Fhost%3A8443';

      // act
      final result = QrPayload.parse(raw);

      // assert
      expect((result as QrParseFailed).errors, [QrError.unsupportedVersion]);
    });

    test('withSeveralProblems_returnsAllErrorsAtOnce', () {
      // arrange
      const raw = 'recam://pair?v=9&f=xyz';

      // act
      final result = QrPayload.parse(raw);

      // assert
      expect((result as QrParseFailed).errors, [
        QrError.unsupportedVersion,
        QrError.missingToken,
        QrError.missingUrl,
        QrError.invalidFingerprint,
      ]);
    });

    test('withForeignQrCode_returnsNotPairingUriError', () {
      // arrange
      const raw = 'https://example.com/?t=abc';

      // act
      final result = QrPayload.parse(raw);

      // assert
      expect((result as QrParseFailed).errors, [QrError.notPairingUri]);
    });
  });
}
