import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

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

/// Task 1 coordinator implementation (tests + `exportOne` via the service).
/// The app no longer auto-starts this; manual Download calls the service.
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

/// Previously started automatic Gallery export. Export now runs only when
/// the player Download button calls [galleryExportServiceProvider].exportOne.
final galleryExportBootstrapProvider = Provider<void>((_) {});

/// Test helper: build a store bound to an injected [SharedPreferences].
GalleryExportStore galleryExportStoreForTest(SharedPreferences prefs) {
  return GalleryExportStore(preferences: prefs);
}
