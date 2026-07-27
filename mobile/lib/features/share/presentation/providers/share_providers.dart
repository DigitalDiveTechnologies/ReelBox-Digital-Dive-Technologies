import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/datasources/android_share_intent_datasource.dart';
import '../../data/datasources/android_share_intent_registrar.dart';
import '../../data/repositories/share_repository_impl.dart';
import '../../domain/repositories/share_repository.dart';
import '../../domain/usecases/receive_shared_url_usecase.dart';
import '../controllers/share_controller.dart';

final androidShareIntentDataSourceProvider =
    Provider<AndroidShareIntentDataSource>((ref) {
  AndroidShareIntentRegistrar.instance.ensureInitialized();
  return AndroidShareIntentRegistrar.instance.dataSource;
});

/// Share repository provider (deep-link + Android share intent).
final shareRepositoryProvider = Provider<ShareRepository>((ref) {
  return ShareRepositoryImpl(
    androidShareIntentDataSource:
        ref.watch(androidShareIntentDataSourceProvider),
  );
});

final receiveSharedUrlUseCaseProvider = Provider<ReceiveSharedUrlUseCase>((ref) {
  return ReceiveSharedUrlUseCase(ref.watch(shareRepositoryProvider));
});

final receiveFromAndroidShareIntentUseCaseProvider =
    Provider<ReceiveFromAndroidShareIntentUseCase>((ref) {
  return ReceiveFromAndroidShareIntentUseCase(
    ref.watch(shareRepositoryProvider),
  );
});

final shareControllerProvider = Provider<ShareController>((ref) {
  return ShareController(
    receiveSharedUrlUseCase: ref.watch(receiveSharedUrlUseCaseProvider),
    receiveFromAndroidShareIntentUseCase: ref.watch(
      receiveFromAndroidShareIntentUseCaseProvider,
    ),
  );
});
