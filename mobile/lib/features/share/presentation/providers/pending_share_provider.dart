import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/router/route_paths.dart';

/// Holds a URL received from Android Share Intent until auth/routing can consume it.
///
/// Prevents Splash/Login `context.go` from dropping the inbound share payload.
final pendingShareUrlProvider = StateProvider<String?>((ref) => null);

/// Completes once cold-start share intake has been attempted (URL or none).
///
/// Splash waits on this so it does not navigate to Home before the MethodChannel
/// returns the ACTION_SEND payload.
final shareIntentBootstrapProvider =
    Provider<ShareIntentBootstrap>((ref) => ShareIntentBootstrap());

class ShareIntentBootstrap {
  final Completer<void> _completer = Completer<void>();

  Future<void> get ready => _completer.future;

  bool get isReady => _completer.isCompleted;

  void markReady() {
    if (!_completer.isCompleted) {
      _completer.complete();
    }
  }
}

/// Builds `/share?url=` for a raw shared URL.
String shareRouteForUrl(String url) {
  return '${RoutePaths.share}?url=${Uri.encodeComponent(url)}';
}
