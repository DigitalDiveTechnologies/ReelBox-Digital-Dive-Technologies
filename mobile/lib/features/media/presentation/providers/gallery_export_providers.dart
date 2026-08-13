import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../../../shared/models/media_item_preview.dart';
import '../../../auth/presentation/providers/auth_providers.dart';
import '../../data/gallery/gallery_export_service.dart';
import '../../data/gallery/gallery_export_store.dart';
import 'media_providers.dart';

final galleryExportStoreProvider = Provider<GalleryExportStore>((ref) {
  return GalleryExportStore();
});

final galleryExportServiceProvider = Provider<GalleryExportService>((ref) {
  return GalleryExportService(
    mediaRepository: ref.watch(mediaRepositoryProvider),
    store: ref.watch(galleryExportStoreProvider),
  );
});

/// App-scoped gallery export coordinator (single source of truth).
///
/// Started/stopped by [galleryExportBootstrapProvider] based on auth session.
final galleryExportCoordinatorProvider =
    Provider<GalleryExportCoordinator>((ref) {
  final local = ref.watch(authLocalDataSourceProvider);
  final coordinator = GalleryExportCoordinator(
    service: ref.watch(galleryExportServiceProvider),
    store: ref.watch(galleryExportStoreProvider),
    isSignedIn: () async {
      final token = await local.getAccessToken();
      return token != null && token.trim().isNotEmpty;
    },
  );
  ref.onDispose(coordinator.dispose);
  return coordinator;
});

/// Keeps the coordinator alive at app root and ties it to auth lifecycle.
///
/// Also listens to existing [mediaListProvider] emissions (Library/Home already
/// refresh this) to kick gallery reconcile as soon as a newly Completed item
/// appears — without changing Library poll intervals or adding a gallery poller.
final galleryExportBootstrapProvider = Provider<void>((ref) {
  final coordinator = ref.watch(galleryExportCoordinatorProvider);

  ref.listen(authNotifierProvider, (previous, next) {
    next.when(
      data: (state) {
        if (state.user != null || state.isAuthenticated) {
          coordinator.onAuthenticated();
        } else {
          coordinator.onLoggedOut();
        }
      },
      loading: () {},
      error: (_, _) {
        coordinator.onLoggedOut();
      },
    );
  });

  ref.listen<AsyncValue<List<MediaItemPreview>>>(mediaListProvider, (
    previous,
    next,
  ) {
    next.whenData((items) {
      unawaited(coordinator.onMediaListSnapshot(items));
    });
  });

  // Cold start: token may exist before AuthNotifier finishes loading user.
  Future<void>(() async {
    final token =
        await ref.read(authLocalDataSourceProvider).getAccessToken();
    if (token != null && token.trim().isNotEmpty) {
      coordinator.onAuthenticated();
    }
  });
});

/// Test helper: build a store bound to an injected [SharedPreferences].
GalleryExportStore galleryExportStoreForTest(SharedPreferences prefs) {
  return GalleryExportStore(preferences: prefs);
}
