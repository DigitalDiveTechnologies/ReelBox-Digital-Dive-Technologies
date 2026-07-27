import '../config/app_config.dart';

/// Resolves signed media URLs from the API for use on device (playback, share, thumbnails).
///
/// The API may return a relative path or a `localhost` base when Docker runs locally;
/// the phone must use [AppConfig.apiBaseUrl] instead.
Uri resolveSignedMediaUrl(String raw) {
  final trimmed = raw.trim();
  if (trimmed.isEmpty) {
    throw ArgumentError('Empty media URL');
  }

  var uri = Uri.parse(trimmed);
  final apiBase = Uri.parse(AppConfig.apiBaseUrl);

  if (!uri.hasScheme || uri.host.isEmpty) {
    uri = apiBase.replace(
      path: uri.path.startsWith('/') ? uri.path : '/${uri.path}',
      query: uri.hasQuery ? uri.query : null,
    );
  } else if (uri.host == 'localhost' || uri.host == '127.0.0.1') {
    uri = uri.replace(
      scheme: apiBase.scheme,
      host: apiBase.host,
      port: apiBase.hasPort ? apiBase.port : null,
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
