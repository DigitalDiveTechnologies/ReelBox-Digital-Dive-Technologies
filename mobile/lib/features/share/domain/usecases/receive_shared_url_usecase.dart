import '../entities/share_request.dart';
import '../repositories/share_repository.dart';

/// Placeholder use case: accept an inbound shared URL.
class ReceiveSharedUrlUseCase {
  const ReceiveSharedUrlUseCase(this._repository);

  final ShareRepository _repository;

  /// Returns a [ShareRequest] for a non-empty URL, otherwise `null`.
  ///
  /// TODO: Later may accept platform-channel payloads (Android/iOS share).
  ShareRequest? call(String? url) {
    return _repository.receiveSharedUrl(url);
  }
}
