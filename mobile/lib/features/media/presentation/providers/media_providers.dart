import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_client_provider.dart';
import '../../../../shared/models/media_item_preview.dart';
import '../../data/datasources/media_remote_datasource.dart';
import '../../data/models/media_dto.dart';
import '../../data/repositories/media_repository_impl.dart';
import '../../domain/repositories/media_repository.dart';

final mediaRemoteDataSourceProvider = Provider<MediaRemoteDataSource>((ref) {
  return MediaRemoteDataSourceImpl(ref.watch(apiClientProvider));
});

final mediaRepositoryProvider = Provider<MediaRepository>((ref) {
  return MediaRepositoryImpl(ref.watch(mediaRemoteDataSourceProvider));
});

/// Single source of truth for Home + Library lists (SRS FR-013).
///
/// Invalidate after create / delete / retry so Library refreshes automatically.
final mediaListProvider =
    FutureProvider.autoDispose<List<MediaItemPreview>>((ref) async {
  final repo = ref.watch(mediaRepositoryProvider);
  final result = await repo.listMedia(page: 1, pageSize: 50);
  return result.items;
});

final mediaDetailProvider =
    FutureProvider.autoDispose.family<MediaItemPreview, String>((ref, id) async {
  return ref.watch(mediaRepositoryProvider).getMedia(id);
});

final mediaPlaybackProvider =
    FutureProvider.autoDispose.family<PlaybackDto, String>((ref, id) async {
  return ref.watch(mediaRepositoryProvider).getPlayback(id);
});
