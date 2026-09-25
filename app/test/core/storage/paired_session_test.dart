import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/storage/credential_store.dart';

import '../../support/fakes.dart';

void main() {
  group('PairedSession.tryParse', () {
    test('withSerializedSession_restoresEveryField', () {
      // arrange
      final source = pairedSession().toJsonString();

      // act
      final session = PairedSession.tryParse(source)!;

      // assert
      expect(session.serverUrl, Uri.parse('https://192.168.0.10:8443'));
      expect(session.fingerprint, fingerprint);
      expect(session.credential, '0123456789abcdef0123456789abcdef.secret');
      expect(session.serverName, 'ReCam');
    });

    test('withCorruptedData_returnsNull', () {
      // arrange
      const source = '{"serverUrl": 3}';

      // act
      final session = PairedSession.tryParse(source);

      // assert
      expect(session, isNull);
    });
  });
}
