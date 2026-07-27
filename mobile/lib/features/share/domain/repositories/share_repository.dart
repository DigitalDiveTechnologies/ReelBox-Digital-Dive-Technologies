import '../entities/share_request.dart';

/// Contract for receiving shared media URLs.
///
/// Android Share Intent feeds URLs through [receiveFromAndroidShareIntent] /
/// [watchAndroidShareIntents]. iOS Share Extension remains deferred.
abstract class ShareRepository {
  /// Builds a [ShareRequest] from an inbound URL string.
  ///
  /// Returns `null` when [url] is null or empty.
  ///
  /// Does not validate, parse, or submit the URL.
  ShareRequest? receiveSharedUrl(String? url);

  /// Reads a pending URL handed off by Android ACTION_SEND (cold start).
  Future<ShareRequest?> receiveFromAndroidShareIntent();

  /// Emits URLs from Android ACTION_SEND while the app is already running.
  Stream<ShareRequest> watchAndroidShareIntents();

  /// TODO: Read a pending URL handed off by iOS Share Extension.
  Future<ShareRequest?> receiveFromIosShareExtension();
}
