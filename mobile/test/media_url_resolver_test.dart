import 'package:flutter_test/flutter_test.dart';
import 'package:mobile/core/config/app_config.dart';
import 'package:mobile/core/network/media_url_resolver.dart';

void main() {
  final apiBase = Uri.parse(AppConfig.apiBaseUrl);
  final expectedHost = apiBase.host;
  final expectedPort = apiBase.hasPort ? apiBase.port : null;

  test('resolveSignedMediaUrl rewrites localhost to API base host', () {
    final uri = resolveSignedMediaUrl(
      'http://localhost:5080/api/v1/media/abc/content?sig=1',
    );
    expect(uri.host, expectedHost);
    expect(uri.port, expectedPort);
    expect(uri.path, '/api/v1/media/abc/content');
    expect(uri.queryParameters['sig'], '1');
  });

  test('resolveSignedMediaUrl prefixes relative paths', () {
    final uri = resolveSignedMediaUrl('/api/v1/media/abc/content?sig=1');
    expect(uri.scheme, apiBase.scheme);
    expect(uri.host, expectedHost);
    expect(uri.path, '/api/v1/media/abc/content');
  });

  test('resolveSignedMediaUrl rewrites stale absolute PublicApiBaseUrl host', () {
    final uri = resolveSignedMediaUrl(
      'https://abdulmutaaltariq-001-site1.gtempurl.com/api/v1/media/abc/content?sig=1',
    );
    expect(uri.scheme, apiBase.scheme);
    expect(uri.host, expectedHost);
    expect(uri.port, expectedPort);
    expect(uri.path, '/api/v1/media/abc/content');
  });
}
