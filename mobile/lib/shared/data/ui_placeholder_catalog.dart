import '../models/media_item_preview.dart';
import '../models/media_platform.dart';
import '../models/media_status.dart';

/// Static placeholder catalog for UI previews only.
///
/// TODO: Replace with GET /api/v1/media once networking is implemented.
abstract final class UiPlaceholderCatalog {
  static final DateTime _now = DateTime.utc(2026, 7, 24, 16, 35);

  static List<MediaItemPreview> get all => [
        MediaItemPreview(
          id: 'demo-completed-1',
          platform: MediaPlatform.instagram,
          status: MediaStatus.completed,
          originalUrl: 'https://www.instagram.com/reel/ABC123/',
          savedAt: _now.subtract(const Duration(hours: 2)),
          title: 'Saved Instagram reel',
          fileSizeLabel: '12.4 MB',
        ),
        MediaItemPreview(
          id: 'demo-completed-2',
          platform: MediaPlatform.facebook,
          status: MediaStatus.completed,
          originalUrl: 'https://www.facebook.com/reel/987654/',
          savedAt: _now.subtract(const Duration(days: 1)),
          title: 'Saved Facebook reel',
          fileSizeLabel: '18.1 MB',
        ),
        MediaItemPreview(
          id: 'demo-downloading',
          platform: MediaPlatform.instagram,
          status: MediaStatus.downloading,
          originalUrl: 'https://www.instagram.com/reel/DL456/',
          savedAt: _now.subtract(const Duration(minutes: 3)),
          title: 'Downloading reel',
          progressPercent: 42,
        ),
        MediaItemPreview(
          id: 'demo-queued',
          platform: MediaPlatform.facebook,
          status: MediaStatus.queued,
          originalUrl: 'https://www.facebook.com/reel/queued01/',
          savedAt: _now.subtract(const Duration(minutes: 1)),
          title: 'Queued reel',
        ),
        MediaItemPreview(
          id: 'demo-preparing',
          platform: MediaPlatform.instagram,
          status: MediaStatus.preparing,
          originalUrl: 'https://www.instagram.com/reel/PREP01/',
          savedAt: _now,
          title: 'Preparing reel',
        ),
        MediaItemPreview(
          id: 'demo-processing',
          platform: MediaPlatform.instagram,
          status: MediaStatus.processing,
          originalUrl: 'https://www.instagram.com/reel/PROC01/',
          savedAt: _now.subtract(const Duration(minutes: 5)),
          title: 'Processing reel',
          progressPercent: 88,
        ),
        MediaItemPreview(
          id: 'demo-failed',
          platform: MediaPlatform.facebook,
          status: MediaStatus.failed,
          originalUrl: 'https://www.facebook.com/reel/fail01/',
          savedAt: _now.subtract(const Duration(hours: 5)),
          title: 'Failed download',
          errorMessage: 'Download could not be completed. You can retry this item.',
        ),
      ];

  static List<MediaItemPreview> get recentCompleted =>
      all.where((m) => m.status == MediaStatus.completed).toList();

  static List<MediaItemPreview> get pending => all
      .where(
        (m) =>
            m.status == MediaStatus.preparing ||
            m.status == MediaStatus.queued ||
            m.status == MediaStatus.downloading ||
            m.status == MediaStatus.processing,
      )
      .toList();

  static List<MediaItemPreview> get failed =>
      all.where((m) => m.status == MediaStatus.failed).toList();

  static MediaItemPreview byId(String id) {
    return all.firstWhere(
      (m) => m.id == id,
      orElse: () => MediaItemPreview(
        id: id.isEmpty ? 'unknown' : id,
        platform: MediaPlatform.instagram,
        status: MediaStatus.completed,
        originalUrl: 'https://www.instagram.com/reel/placeholder/',
        savedAt: _now,
        title: 'Media item',
        fileSizeLabel: '—',
      ),
    );
  }
}
