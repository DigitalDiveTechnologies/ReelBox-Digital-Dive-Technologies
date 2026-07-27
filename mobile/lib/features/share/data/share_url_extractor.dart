/// Safely extracts the first http(s) URL from Android share-sheet text.
///
/// Instagram / Facebook often wrap the reel link in surrounding copy; this
/// helper isolates a URL without validating platform or media type.
class ShareUrlExtractor {
  const ShareUrlExtractor._();

  static final RegExp _urlPattern = RegExp(
    r'https?://[^\s<>"\]\)\}]+',
    caseSensitive: false,
  );

  /// Returns a trimmed http(s) URL, or `null` when none is present.
  static String? extract(String? raw) {
    if (raw == null) return null;

    // Instagram sometimes inserts zero-width / BOM characters around links.
    final trimmed = raw
        .replaceAll(RegExp(r'[\u200B-\u200D\uFEFF]'), '')
        .trim();
    if (trimmed.isEmpty) return null;

    final asUri = Uri.tryParse(trimmed);
    if (_isHttpUrl(asUri)) {
      return trimmed;
    }

    final match = _urlPattern.firstMatch(trimmed);
    if (match == null) return null;

    final candidate = _stripTrailingPunctuation(match.group(0)!);
    final matchedUri = Uri.tryParse(candidate);
    if (!_isHttpUrl(matchedUri)) return null;

    return candidate;
  }

  static bool _isHttpUrl(Uri? uri) {
    if (uri == null) return false;
    if (uri.scheme != 'http' && uri.scheme != 'https') return false;
    return uri.host.isNotEmpty;
  }

  static String _stripTrailingPunctuation(String value) {
    return value.replaceAll(RegExp(r'[.,;:!?)]+$'), '');
  }
}
