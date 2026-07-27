import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_platform.dart';
import '../../domain/repositories/media_repository.dart';
import '../datasources/media_remote_datasource.dart';
import '../models/media_dto.dart';

class MediaRepositoryImpl implements MediaRepository {
  MediaRepositoryImpl(this._remote);

  final MediaRemoteDataSource _remote;

  @override
  Future<MediaItemPreview> createMedia({
    required String url,
    String? source,
  }) async {
    final dto = await _remote.createMedia(url: url, source: source);
    return dto.toPreview();
  }

  @override
  Future<({List<MediaItemPreview> items, int totalCount})> listMedia({
    int page = 1,
    int pageSize = 20,
    MediaPlatform? platform,
    String? status,
  }) async {
    final dto = await _remote.listMedia(
      page: page,
      pageSize: pageSize,
      platform: platform?.name,
      status: status,
    );
    return (
      items: dto.items.map((e) => e.toPreview()).toList(growable: false),
      totalCount: dto.totalCount,
    );
  }

  @override
  Future<MediaItemPreview> getMedia(String id) async {
    final dto = await _remote.getMedia(id);
    return dto.toPreview();
  }

  @override
  Future<MediaItemPreview> retryMedia(String id) async {
    final dto = await _remote.retryMedia(id);
    return dto.toPreview();
  }

  @override
  Future<void> deleteMedia(String id) => _remote.deleteMedia(id);

  @override
  Future<PlaybackDto> getPlayback(String id) => _remote.getPlayback(id);
}
