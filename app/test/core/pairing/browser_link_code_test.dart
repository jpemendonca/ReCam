import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/pairing/browser_link_code.dart';

const _linkId = '0123456789abcdef0123456789abcdef';

void main() {
  group('BrowserLinkCode.tryParse', () {
    test('withConnectBrowserCode_readsLinkAndSecret', () {
      // act
      final code = BrowserLinkCode.tryParse(
        'recam://connect-browser?v=1&l=$_linkId&s=abc_DEF-123',
      );

      // assert
      expect(code?.linkId, _linkId);
      expect(code?.secret, 'abc_DEF-123');
    });

    test('withPairingCode_returnsNull', () {
      // act
      final code = BrowserLinkCode.tryParse(
        'recam://pair?v=1&t=token&r=camera&u=https%3A%2F%2F192.168.0.10%3A8443',
      );

      // assert
      expect(code, isNull);
    });

    test('withOtherVersionOrMissingParts_returnsNull', () {
      // act
      final codes = [
        'recam://connect-browser?v=2&l=$_linkId&s=abc',
        'recam://connect-browser?v=1&l=short&s=abc',
        'recam://connect-browser?v=1&l=$_linkId',
        'not a code',
      ].map(BrowserLinkCode.tryParse);

      // assert
      expect(codes, everyElement(isNull));
    });
  });
}
