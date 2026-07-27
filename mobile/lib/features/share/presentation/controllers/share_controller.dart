import '../../domain/entities/share_request.dart';
import '../../domain/usecases/receive_shared_url_usecase.dart';

/// Presentation controller for share intake.
class ShareController {
  ShareController({
    required this._receiveSharedUrlUseCase,
    required this._receiveFromAndroidShareIntentUseCase,
  });

  final ReceiveSharedUrlUseCase _receiveSharedUrlUseCase;
  final ReceiveFromAndroidShareIntentUseCase
      _receiveFromAndroidShareIntentUseCase;

  /// Resolves a [ShareRequest] from a route/deep-link URL query value.
  ShareRequest? receiveSharedUrl(String? url) {
    return _receiveSharedUrlUseCase(url);
  }

  /// Cold-start Android ACTION_SEND intake.
  Future<ShareRequest?> receiveFromAndroidShareIntent() {
    return _receiveFromAndroidShareIntentUseCase();
  }

  /// Warm Android ACTION_SEND intake while the app is running.
  Stream<ShareRequest> watchAndroidShareIntents() {
    return _receiveFromAndroidShareIntentUseCase.watch();
  }
}
