/// A URL received through share / deep-link intake.
///
/// No validation or platform parsing is performed in this sprint.
class ShareRequest {
  const ShareRequest({
    required this.url,
    this.source,
  });

  /// Raw shared URL string as delivered by the entry path.
  final String url;

  /// Optional intake channel label (e.g. deep_link, share_sheet).
  final String? source;
}
