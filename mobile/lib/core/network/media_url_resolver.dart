import '../config/app_config.dart';

/// Resolves signed media URLs from the API for use on device (playback, share, thumbnails).
///
/// The API may return a relative path, a `localhost` base, or a stale absolute host
/// (wrong PublicApiBaseUrl). The phone must always use [AppConfig.apiBaseUrl].
///
/// Basic-auth credentials embedded in [AppConfig.apiBaseUrl] are never copied into
/// media URLs (players/caches must not receive host passwords).
Uri resolveSignedMediaUrl(String raw) {
  final trimmed = raw.trim();
  if (trimmed.isEmpty) {
    throw ArgumentError('Empty media URL');
  }

  var uri = Uri.parse(trimmed);
  final apiBase = Uri.parse(AppConfig.apiBaseUrl);
  final publicBase = Uri(
    scheme: apiBase.scheme,
    host: apiBase.host,
    port: apiBase.hasPort ? apiBase.port : null,
  );

  final isMediaApiPath = uri.path.contains('/api/v1/media/');
  final needsHostRewrite = !uri.hasScheme ||
      uri.host.isEmpty ||
      uri.host == 'localhost' ||
      uri.host == '127.0.0.1' ||
      (isMediaApiPath &&
          (uri.host != publicBase.host ||
              (publicBase.hasPort && uri.port != publicBase.port) ||
              (!publicBase.hasPort && uri.hasPort)));

  if (needsHostRewrite) {
    final path = !uri.hasScheme || uri.host.isEmpty
        ? (uri.path.startsWith('/') ? uri.path : '/${uri.path}')
        : uri.path;
    uri = publicBase.replace(
      path: path,
      query: uri.hasQuery ? uri.query : null,
    );
  }

  return uri;
}

/// Returns `true` when [url] can be loaded after resolution.
bool isResolvableMediaUrl(String? url) {
  if (url == null || url.trim().isEmpty) return false;
  try {
    resolveSignedMediaUrl(url);
    return true;
  } catch (_) {
    return false;
  }
}
