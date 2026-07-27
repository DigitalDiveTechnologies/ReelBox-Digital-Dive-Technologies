import '../../domain/entities/share_request.dart';
import '../../domain/repositories/share_repository.dart';
import '../datasources/android_share_intent_datasource.dart';
import '../share_url_extractor.dart';

/// [ShareRepository] implementation for deep-link and Android share intake.
///
/// Packages inbound URL strings only — no API, validation, or media logic.
class ShareRepositoryImpl implements ShareRepository {
  ShareRepositoryImpl({
    AndroidShareIntentDataSource? androidShareIntentDataSource,
  }) : _androidShareIntentDataSource =
            androidShareIntentDataSource ?? AndroidShareIntentDataSource();

  final AndroidShareIntentDataSource _androidShareIntentDataSource;

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
    final text = await _androidShareIntentDataSource.getInitialSharedText();
    return _requestFromSharedText(text);
  }

  @override
  Stream<ShareRequest> watchAndroidShareIntents() {
    return _androidShareIntentDataSource.watchSharedText()
        .map(_requestFromSharedText)
        .where((ShareRequest? request) => request != null)
        .map((ShareRequest? request) => request!);
  }

  @override
  Future<ShareRequest?> receiveFromIosShareExtension() async {
    // TODO: Implement iOS Share Extension handoff into the main app.
    return null;
  }

  ShareRequest? _requestFromSharedText(String? text) {
    final url = ShareUrlExtractor.extract(text);
    if (url == null) return null;

    return ShareRequest(
      url: url,
      source: 'android_share_intent',
    );
  }
}
