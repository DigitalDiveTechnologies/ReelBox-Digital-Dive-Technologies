/// Client-side validation for Instagram / Facebook reel URLs.
///
/// Aligns with backend MediaUrlAnalyzer hosts and yt-dlp supported paths.
class SupportedReelUrlValidator {
  const SupportedReelUrlValidator._();

  static final RegExp _urlPattern = RegExp(
    r'https?://[^\s<>"\]\)\}]+',
    caseSensitive: false,
  );

  static final RegExp _instagramPath = RegExp(
    r'^/(reel|p|tv)/[^/]+/?$',
    caseSensitive: false,
  );

  /// Matches yt-dlp Facebook paths in YtDlpMediaResolver.IsSupportedContentUrl.
  static final RegExp _facebookPath = RegExp(
    r'^/(reel|reels|videos|watch|share/v|share/r)/',
    caseSensitive: false,
  );

  static final RegExp _facebookShareShortPath = RegExp(
    r'^/share/[^/]+/?$',
    caseSensitive: false,
  );

  /// Extracts and validates a supported reel URL from raw text.
  static SupportedReelUrlResult validate(String? raw) {
    if (raw == null || raw.trim().isEmpty) {
      return const SupportedReelUrlResult.error(
        'Paste an Instagram or Facebook reel link.',
      );
    }

    final cleaned = raw
        .replaceAll(RegExp(r'[\u200B-\u200D\uFEFF]'), '')
        .trim();

    final candidate = _extractUrl(cleaned);
    if (candidate == null) {
      return const SupportedReelUrlResult.error('Enter a valid http(s) link.');
    }

    final uri = Uri.tryParse(candidate);
    if (uri == null ||
        (uri.scheme != 'http' && uri.scheme != 'https') ||
        uri.host.isEmpty) {
      return const SupportedReelUrlResult.error('Enter a valid http(s) link.');
    }

    final host = uri.host.toLowerCase();
    if (_isInstagramHost(host)) {
      final path = uri.path.isEmpty ? '/' : uri.path;
      if (!_instagramPath.hasMatch(path)) {
        return const SupportedReelUrlResult.error(
          'Use an Instagram reel, post, or TV link (e.g. /reel/…).',
        );
      }
      return SupportedReelUrlResult.ok(candidate);
    }

    if (_isFacebookHost(host)) {
      if (host.contains('fb.watch')) {
        return SupportedReelUrlResult.ok(candidate);
      }
      final path = uri.path.isEmpty ? '/' : uri.path;
      if (_facebookPath.hasMatch(path) ||
          _facebookShareShortPath.hasMatch(path) ||
          uri.queryParameters.containsKey('v')) {
        return SupportedReelUrlResult.ok(candidate);
      }
      return const SupportedReelUrlResult.error(
        'Use a Facebook reel, watch, or video link.',
      );
    }

    return const SupportedReelUrlResult.error(
      'Only Instagram and Facebook reel links are supported.',
    );
  }

  static String? _extractUrl(String trimmed) {
    final asUri = Uri.tryParse(trimmed);
    if (asUri != null &&
        (asUri.scheme == 'http' || asUri.scheme == 'https') &&
        asUri.host.isNotEmpty) {
      return trimmed;
    }

    final match = _urlPattern.firstMatch(trimmed);
    if (match == null) return null;
    return match.group(0)!.replaceAll(RegExp(r'[.,;:!?)]+$'), '');
  }

  static bool _isInstagramHost(String host) {
    return host == 'instagram.com' ||
        host == 'www.instagram.com' ||
        host == 'm.instagram.com' ||
        host == 'instagr.am' ||
        host.endsWith('.instagram.com');
  }

  static bool _isFacebookHost(String host) {
    return host == 'facebook.com' ||
        host == 'www.facebook.com' ||
        host == 'm.facebook.com' ||
        host == 'fb.com' ||
        host == 'www.fb.com' ||
        host == 'fb.watch' ||
        host == 'www.fb.watch' ||
        host.endsWith('.facebook.com') ||
        host.endsWith('.fb.com');
  }
}

class SupportedReelUrlResult {
  const SupportedReelUrlResult._({this.url, this.errorMessage});

  const SupportedReelUrlResult.ok(String url)
      : this._(url: url, errorMessage: null);

  const SupportedReelUrlResult.error(String message)
      : this._(url: null, errorMessage: message);

  final String? url;
  final String? errorMessage;

  bool get isValid => url != null && url!.isNotEmpty;
}
