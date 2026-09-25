import 'package:flutter_test/flutter_test.dart';
import 'package:recam/core/pairing/device_name_validator.dart';

void main() {
  group('DeviceNameValidator.validate', () {
    test('withOrdinaryName_returnsNoErrors', () {
      // arrange
      const name = 'Sala de estar';

      // act
      final errors = DeviceNameValidator.validate(name);

      // assert
      expect(errors, isEmpty);
    });

    test('withBlankName_returnsEmpty', () {
      // arrange
      const name = '   ';

      // act
      final errors = DeviceNameValidator.validate(name);

      // assert
      expect(errors, [DeviceNameError.empty]);
    });

    test('withFortyOneCharacters_returnsTooLong', () {
      // arrange
      final name = 'x' * 41;

      // act
      final errors = DeviceNameValidator.validate(name);

      // assert
      expect(errors, [DeviceNameError.tooLong]);
    });

    test('withFortyCharactersPlusSpaces_returnsNoErrors', () {
      // arrange
      final name = ' ${'x' * 40} ';

      // act
      final errors = DeviceNameValidator.validate(name);

      // assert
      expect(errors, isEmpty);
    });

    test('withControlCharacter_returnsControlCharacters', () {
      // arrange
      const name = 'Kit\u0007chen';

      // act
      final errors = DeviceNameValidator.validate(name);

      // assert
      expect(errors, [DeviceNameError.controlCharacters]);
    });
  });
}
