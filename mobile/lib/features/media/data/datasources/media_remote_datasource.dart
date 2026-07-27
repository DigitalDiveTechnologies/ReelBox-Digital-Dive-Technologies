import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import '../models/media_dto.dart';

abstract class MediaRemoteDataSource {
  Future<MediaDto> createMedia({
    required String url,
    String? source,
  });

  Future<MediaListDto> listMedia({
    int page = 1,
    int pageSize = 20,
    String? status,
    String? platform,
  });

  Future<MediaDto> getMedia(String id);

  Future<MediaDto> retryMedia(String id);

  Future<void> deleteMedia(String id);

  Future<PlaybackDto> getPlayback(String id);
}

class MediaRemoteDataSourceImpl implements MediaRemoteDataSource {
  MediaRemoteDataSourceImpl(this._api);

  final ApiClient _api;

  @override
  Future<MediaDto> createMedia({
    required String url,
    String? source,
  }) async {
    final json = await _api.postJson(
      ApiEndpoints.media,
      body: {
        'url': url,
        'source': ?source,
      },
    );
    return MediaDto.fromJson(json);
  }

  @override
  Future<MediaListDto> listMedia({
    int page = 1,
    int pageSize = 20,
    String? status,
    String? platform,
  }) async {
    final query = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
      if (status != null && status.isNotEmpty) 'status': status,
      if (platform != null && platform.isNotEmpty) 'platform': platform,
    };
    final json = await _api.getJson(ApiEndpoints.media, queryParameters: query);
    return MediaListDto.fromJson(json);
  }

  @override
  Future<MediaDto> getMedia(String id) async {
    final json = await _api.getJson(ApiEndpoints.mediaById(id));
    return MediaDto.fromJson(json);
  }

  @override
  Future<MediaDto> retryMedia(String id) async {
    final json = await _api.postJson(ApiEndpoints.mediaRetry(id));
    return MediaDto.fromJson(json);
  }

  @override
  Future<void> deleteMedia(String id) {
    return _api.deleteJson(ApiEndpoints.mediaById(id));
  }

  @override
  Future<PlaybackDto> getPlayback(String id) async {
    final json = await _api.getJson(ApiEndpoints.mediaPlayback(id));
    return PlaybackDto.fromJson(json);
  }
}
