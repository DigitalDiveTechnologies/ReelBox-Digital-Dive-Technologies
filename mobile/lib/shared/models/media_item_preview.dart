import 'media_platform.dart';
import 'media_status.dart';

/// UI-only media preview model.
///
/// Not wired to backend APIs — used for layout placeholders (Sprint F1).
class MediaItemPreview {
  const MediaItemPreview({
    required this.id,
    required this.platform,
    required this.status,
    required this.originalUrl,
    required this.savedAt,
    this.title,
    this.fileSizeLabel,
    this.progressPercent,
    this.errorMessage,
  });

  final String id;
  final MediaPlatform platform;
  final MediaStatus status;
  final String originalUrl;
  final DateTime savedAt;
  final String? title;
  final String? fileSizeLabel;
  final int? progressPercent;
  final String? errorMessage;

  String get displayTitle =>
      title ?? (platform == MediaPlatform.instagram ? 'Instagram reel' : 'Facebook reel');
}
