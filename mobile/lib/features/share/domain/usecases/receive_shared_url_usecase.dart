import '../../domain/entities/share_request.dart';
import '../../domain/repositories/share_repository.dart';

/// Accepts an inbound shared URL from a deep-link / route query.
class ReceiveSharedUrlUseCase {
  const ReceiveSharedUrlUseCase(this._repository);

  final ShareRepository _repository;

  /// Returns a [ShareRequest] for a non-empty URL, otherwise `null`.
  ShareRequest? call(String? url) {
    return _repository.receiveSharedUrl(url);
  }
}

/// Reads a URL delivered by Android Share Intent (ACTION_SEND text/plain).
class ReceiveFromAndroidShareIntentUseCase {
  const ReceiveFromAndroidShareIntentUseCase(this._repository);

  final ShareRepository _repository;

  /// Cold-start / one-shot Android share payload.
  Future<ShareRequest?> call() {
    return _repository.receiveFromAndroidShareIntent();
  }

  /// Warm shares while the Android process is already alive.
  Stream<ShareRequest> watch() {
    return _repository.watchAndroidShareIntents();
  }
}
