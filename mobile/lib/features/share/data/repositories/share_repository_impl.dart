import '../../domain/entities/share_request.dart';
import '../../domain/repositories/share_repository.dart';

/// Placeholder [ShareRepository] implementation.
///
/// Packages inbound URL strings only — no API, validation, or media logic.
class ShareRepositoryImpl implements ShareRepository {
  const ShareRepositoryImpl();

  @override
  ShareRequest? receiveSharedUrl(String? url) {
    if (url == null || url.trim().isEmpty) {
      return null;
    }

    return ShareRequest(
      url: url.trim(),
      source: 'deep_link',
    );
  }

  @override
  Future<ShareRequest?> receiveFromAndroidShareIntent() async {
    // TODO: Implement Android Share Intent / ACTION_SEND text intake.
    return null;
  }

  @override
  Future<ShareRequest?> receiveFromIosShareExtension() async {
    // TODO: Implement iOS Share Extension handoff into the main app.
    return null;
  }
}
