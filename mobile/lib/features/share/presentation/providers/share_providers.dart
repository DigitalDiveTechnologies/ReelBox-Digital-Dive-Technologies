import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../data/repositories/share_repository_impl.dart';
import '../../domain/repositories/share_repository.dart';
import '../../domain/usecases/receive_shared_url_usecase.dart';
import '../controllers/share_controller.dart';

/// Share repository provider.
///
/// TODO: Inject platform channels when Android/iOS share intake is wired.
final shareRepositoryProvider = Provider<ShareRepository>((ref) {
  return const ShareRepositoryImpl();
});

final receiveSharedUrlUseCaseProvider = Provider<ReceiveSharedUrlUseCase>((ref) {
  return ReceiveSharedUrlUseCase(ref.watch(shareRepositoryProvider));
});

final shareControllerProvider = Provider<ShareController>((ref) {
  return ShareController(ref.watch(receiveSharedUrlUseCaseProvider));
});
