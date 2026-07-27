import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_platform.dart';
import '../../data/models/media_dto.dart';

abstract class MediaRepository {
  Future<MediaItemPreview> createMedia({
    required String url,
    String? source,
  });

  Future<({List<MediaItemPreview> items, int totalCount})> listMedia({
    int page = 1,
    int pageSize = 20,
    MediaPlatform? platform,
    String? status,
  });

  Future<MediaItemPreview> getMedia(String id);

  Future<MediaItemPreview> retryMedia(String id);

  Future<void> deleteMedia(String id);

  Future<PlaybackDto> getPlayback(String id);
}
