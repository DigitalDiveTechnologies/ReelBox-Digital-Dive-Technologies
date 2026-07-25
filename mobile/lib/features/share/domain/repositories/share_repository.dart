import '../entities/share_request.dart';

/// Contract for receiving shared media URLs.
///
/// TODO: Android Share Intent integration will feed URLs through this contract.
/// TODO: iOS Share Extension integration will feed URLs through this contract.
abstract class ShareRepository {
  /// Builds a [ShareRequest] from an inbound URL string.
  ///
  /// Returns `null` when [url] is null or empty.
  ///
  /// Does not validate, parse, or submit the URL.
  ShareRequest? receiveSharedUrl(String? url);

  /// TODO: Read a pending URL handed off by Android Share Intent.
  Future<ShareRequest?> receiveFromAndroidShareIntent();

  /// TODO: Read a pending URL handed off by iOS Share Extension.
  Future<ShareRequest?> receiveFromIosShareExtension();
}
