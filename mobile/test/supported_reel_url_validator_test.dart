import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/features/home/data/supported_reel_url_validator.dart';

void main() {
  test('accepts Instagram reel URL', () {
    final result = SupportedReelUrlValidator.validate(
      'https://www.instagram.com/reel/ABC123/',
    );
    expect(result.isValid, isTrue);
    expect(result.url, contains('instagram.com/reel/ABC123'));
  });

  test('rejects non-platform URL', () {
    final result = SupportedReelUrlValidator.validate(
      'https://example.com/video/1',
    );
    expect(result.isValid, isFalse);
    expect(result.errorMessage, contains('Instagram and Facebook'));
  });

  test('accepts Facebook watch URL', () {
    final result = SupportedReelUrlValidator.validate(
      'https://www.facebook.com/watch/?v=123456',
    );
    expect(result.isValid, isTrue);
  });

  test('accepts Facebook reel URL', () {
    final result = SupportedReelUrlValidator.validate(
      'https://www.facebook.com/reel/123456789/',
    );
    expect(result.isValid, isTrue);
  });

  test('accepts Facebook share/r URL', () {
    final result = SupportedReelUrlValidator.validate(
      'https://www.facebook.com/share/r/1AbCdEfG/',
    );
    expect(result.isValid, isTrue);
  });

  test('accepts Facebook share/v URL', () {
    final result = SupportedReelUrlValidator.validate(
      'https://www.facebook.com/share/v/1AbCdEfG/',
    );
    expect(result.isValid, isTrue);
  });

  test('accepts fb.watch URL', () {
    final result = SupportedReelUrlValidator.validate(
      'https://fb.watch/abcXYZ/',
    );
    expect(result.isValid, isTrue);
  });

  test('rejects empty input', () {
    final result = SupportedReelUrlValidator.validate('   ');
    expect(result.isValid, isFalse);
  });
}
