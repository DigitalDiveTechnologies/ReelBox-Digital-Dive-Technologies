import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_platform.dart';
import '../../../../shared/models/media_status.dart';

class MediaDto {
  const MediaDto({
    required this.id,
    required this.platform,
    required this.status,
    required this.originalUrl,
    required this.createdAt,
    required this.updatedAt,
    required this.retryCount,
    this.normalizedUrl,
    this.title,
    this.thumbnailStorageKey,
    this.thumbnailUrl,
    this.mediaStorageKey,
    this.mimeType,
    this.fileSizeBytes,
    this.durationMs,
    this.progressPercent,
    this.downloadStartedAt,
    this.downloadedAt,
    this.errorCode,
    this.errorMessage,
    this.source,
  });

  final String id;
  final String platform;
  final String status;
  final String originalUrl;
  final String? normalizedUrl;
  final String? title;
  final String? thumbnailStorageKey;
  final String? thumbnailUrl;
  final String? mediaStorageKey;
  final String? mimeType;
  final int? fileSizeBytes;
  final int? durationMs;
  final int? progressPercent;
  final DateTime createdAt;
  final DateTime? downloadStartedAt;
  final DateTime? downloadedAt;
  final DateTime updatedAt;
  final String? errorCode;
  final String? errorMessage;
  final int retryCount;
  final String? source;

  factory MediaDto.fromJson(Map<String, dynamic> json) {
    return MediaDto(
      id: json['id']?.toString() ?? '',
      platform: json['platform']?.toString() ?? 'instagram',
      status: json['status']?.toString() ?? 'preparing',
      originalUrl: json['originalUrl']?.toString() ?? '',
      normalizedUrl: json['normalizedUrl']?.toString(),
      title: json['title']?.toString(),
      thumbnailStorageKey: json['thumbnailStorageKey']?.toString(),
      thumbnailUrl: json['thumbnailUrl']?.toString(),
      mediaStorageKey: json['mediaStorageKey']?.toString(),
      mimeType: json['mimeType']?.toString(),
      fileSizeBytes: (json['fileSizeBytes'] as num?)?.toInt(),
      durationMs: (json['durationMs'] as num?)?.toInt(),
      progressPercent: (json['progressPercent'] as num?)?.toInt(),
      createdAt: DateTime.tryParse(json['createdAt']?.toString() ?? '') ??
          DateTime.now().toUtc(),
      downloadStartedAt:
          DateTime.tryParse(json['downloadStartedAt']?.toString() ?? ''),
      downloadedAt: DateTime.tryParse(json['downloadedAt']?.toString() ?? ''),
      updatedAt: DateTime.tryParse(json['updatedAt']?.toString() ?? '') ??
          DateTime.now().toUtc(),
      errorCode: json['errorCode']?.toString(),
      errorMessage: json['errorMessage']?.toString(),
      retryCount: (json['retryCount'] as num?)?.toInt() ?? 0,
      source: json['source']?.toString(),
    );
  }

  MediaItemPreview toPreview() {
    final created = createdAt.toLocal();
    return MediaItemPreview(
      id: id,
      platform: _platformFromApi(platform),
      status: _statusFromApi(status),
      originalUrl: originalUrl,
      createdAt: created,
      savedAt: (downloadedAt ?? createdAt).toLocal(),
      title: title,
      fileSizeLabel: _formatBytes(fileSizeBytes),
      progressPercent: progressPercent,
      errorMessage: errorMessage,
      thumbnailStorageKey: thumbnailStorageKey,
      thumbnailUrl: thumbnailUrl,
      mediaStorageKey: mediaStorageKey,
      mimeType: mimeType,
    );
  }
}

class MediaListDto {
  const MediaListDto({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.totalPages,
  });

  final List<MediaDto> items;
  final int page;
  final int pageSize;
  final int totalCount;
  final int totalPages;

  factory MediaListDto.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'];
    final items = <MediaDto>[];
    if (rawItems is List) {
      for (final entry in rawItems) {
        if (entry is Map<String, dynamic>) {
          items.add(MediaDto.fromJson(entry));
        }
      }
    }

    return MediaListDto(
      items: items,
      page: (json['page'] as num?)?.toInt() ?? 1,
      pageSize: (json['pageSize'] as num?)?.toInt() ?? 20,
      totalCount: (json['totalCount'] as num?)?.toInt() ?? items.length,
      totalPages: (json['totalPages'] as num?)?.toInt() ?? 1,
    );
  }
}

class PlaybackDto {
  const PlaybackDto({
    required this.mediaId,
    required this.status,
    required this.delivery,
    this.mediaStorageKey,
    this.thumbnailStorageKey,
    this.thumbnailUrl,
    this.mimeType,
    this.playbackUrl,
    this.expiresAt,
  });

  final String mediaId;
  final String status;
  final String? mediaStorageKey;
  final String? thumbnailStorageKey;
  final String? thumbnailUrl;
  final String? mimeType;
  final String? playbackUrl;
  final String delivery;
  final DateTime? expiresAt;

  factory PlaybackDto.fromJson(Map<String, dynamic> json) {
    return PlaybackDto(
      mediaId: json['mediaId']?.toString() ?? '',
      status: json['status']?.toString() ?? '',
      mediaStorageKey: json['mediaStorageKey']?.toString(),
      thumbnailStorageKey: json['thumbnailStorageKey']?.toString(),
      thumbnailUrl: json['thumbnailUrl']?.toString(),
      mimeType: json['mimeType']?.toString(),
      playbackUrl: json['playbackUrl']?.toString(),
      delivery: json['delivery']?.toString() ?? '',
      expiresAt: DateTime.tryParse(json['expiresAt']?.toString() ?? ''),
    );
  }
}

MediaPlatform _platformFromApi(String value) {
  switch (value.toLowerCase()) {
    case 'facebook':
      return MediaPlatform.facebook;
    default:
      return MediaPlatform.instagram;
  }
}

MediaStatus _statusFromApi(String value) {
  switch (value.toLowerCase()) {
    case 'queued':
      return MediaStatus.queued;
    case 'downloading':
      return MediaStatus.downloading;
    case 'processing':
      return MediaStatus.processing;
    case 'completed':
      return MediaStatus.completed;
    case 'failed':
      return MediaStatus.failed;
    default:
      return MediaStatus.preparing;
  }
}

String? _formatBytes(int? bytes) {
  if (bytes == null || bytes <= 0) return null;
  if (bytes < 1024) return '$bytes B';
  final kb = bytes / 1024;
  if (kb < 1024) return '${kb.toStringAsFixed(0)} KB';
  final mb = kb / 1024;
  return '${mb.toStringAsFixed(1)} MB';
}
