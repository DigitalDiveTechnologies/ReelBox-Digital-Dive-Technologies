import 'media_platform.dart';
import 'media_status.dart';

/// Media preview mapped from backend `MediaResponse` (SRS §12 / §9).
class MediaItemPreview {
  const MediaItemPreview({
    required this.id,
    required this.platform,
    required this.status,
    required this.originalUrl,
    required this.createdAt,
    required this.savedAt,
    this.title,
    this.fileSizeLabel,
    this.progressPercent,
    this.errorMessage,
    this.thumbnailStorageKey,
    this.thumbnailUrl,
    this.mediaStorageKey,
    this.mimeType,
  });

  final String id;
  final MediaPlatform platform;
  final MediaStatus status;
  final String originalUrl;

  /// Request accepted time from backend `createdAt`.
  final DateTime createdAt;

  /// Best-effort display timestamp (`downloadedAt` or `createdAt`).
  final DateTime savedAt;
  final String? title;
  final String? fileSizeLabel;
  final int? progressPercent;
  final String? errorMessage;
  final String? thumbnailStorageKey;

  /// Signed thumbnail delivery URL when available (SRS FR-010).
  final String? thumbnailUrl;
  final String? mediaStorageKey;
  final String? mimeType;

  bool get hasThumbnailKey =>
      thumbnailStorageKey != null && thumbnailStorageKey!.trim().isNotEmpty;

  bool get hasThumbnailUrl =>
      thumbnailUrl != null && thumbnailUrl!.trim().isNotEmpty;

  bool get isActive =>
      status == MediaStatus.preparing ||
      status == MediaStatus.queued ||
      status == MediaStatus.downloading ||
      status == MediaStatus.processing;

  String get displayTitle =>
      title ??
      (platform == MediaPlatform.instagram ? 'Instagram reel' : 'Facebook reel');

  String get createdDateLabel {
    final local = createdAt.toLocal();
    final y = local.year.toString().padLeft(4, '0');
    final m = local.month.toString().padLeft(2, '0');
    final d = local.day.toString().padLeft(2, '0');
    return '$y-$m-$d';
  }
}
