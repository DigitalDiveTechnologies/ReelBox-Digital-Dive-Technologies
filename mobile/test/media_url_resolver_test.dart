import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/network/media_url_resolver.dart';

void main() {
  test('resolveSignedMediaUrl rewrites localhost to API base host', () {
    final uri = resolveSignedMediaUrl(
      'http://localhost:5080/api/v1/media/abc/content?sig=1',
    );
    expect(uri.host, '192.168.100.8');
    expect(uri.port, 5080);
    expect(uri.path, '/api/v1/media/abc/content');
  });

  test('resolveSignedMediaUrl prefixes relative paths', () {
    final uri = resolveSignedMediaUrl('/api/v1/media/abc/content?sig=1');
    expect(uri.scheme, 'http');
    expect(uri.host, '192.168.100.8');
    expect(uri.path, '/api/v1/media/abc/content');
  });
}
