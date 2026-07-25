import '../../domain/entities/share_request.dart';
import '../../domain/usecases/receive_shared_url_usecase.dart';

/// Presentation controller for share intake.
class ShareController {
  ShareController(this._receiveSharedUrlUseCase);

  final ReceiveSharedUrlUseCase _receiveSharedUrlUseCase;

  /// Resolves a [ShareRequest] from a route/deep-link URL query value.
  ///
  /// TODO: Also accept URLs from Android Share Intent / iOS Share Extension.
  ShareRequest? receiveSharedUrl(String? url) {
    return _receiveSharedUrlUseCase(url);
  }
}
