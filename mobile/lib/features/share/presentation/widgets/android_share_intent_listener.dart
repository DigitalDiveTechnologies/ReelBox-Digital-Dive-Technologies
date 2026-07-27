import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/router/app_router_provider.dart';
import '../../../../core/router/route_paths.dart';
import '../../domain/entities/share_request.dart';
import '../providers/pending_share_provider.dart';
import '../providers/share_providers.dart';

/// Listens for Android share intents and opens `/share?url=`.
///
/// Pending URLs are stored so Splash/Login cannot drop the share payload.
class AndroidShareIntentListener extends ConsumerStatefulWidget {
  const AndroidShareIntentListener({
    super.key,
    required this.child,
  });

  final Widget child;

  @override
  ConsumerState<AndroidShareIntentListener> createState() =>
      _AndroidShareIntentListenerState();
}

class _AndroidShareIntentListenerState
    extends ConsumerState<AndroidShareIntentListener> {
  StreamSubscription<ShareRequest>? _subscription;
  bool _started = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_started) return;
    _started = true;
    unawaited(_start());
  }

  Future<void> _start() async {
    final bootstrap = ref.read(shareIntentBootstrapProvider);
    final controller = ref.read(shareControllerProvider);

    try {
      _subscription = controller.watchAndroidShareIntents().listen(_onShare);

      // Subscribe before cold-read so warm onNewIntent cannot land on a dead stream.
      await Future<void>.delayed(Duration.zero);

      final initial = await controller.receiveFromAndroidShareIntent();
      if (!mounted) return;
      if (kDebugMode && initial != null) {
        debugPrint('SHARE_INTENT: cold-start url=${initial.url}');
      }
      if (initial != null) {
        _onShare(initial);
      }
    } finally {
      bootstrap.markReady();
    }
  }

  void _onShare(ShareRequest request) {
    final url = request.url.trim();
    if (url.isEmpty) return;

    if (kDebugMode) {
      debugPrint('SHARE_INTENT: received url=$url');
    }

    ref.read(pendingShareUrlProvider.notifier).state = url;

    final router = ref.read(appRouterProvider);
    final path = router.routerDelegate.currentConfiguration.uri.path;

    // Splash/Login/Register own the next navigation — do not race them.
    if (path == RoutePaths.splash ||
        path == RoutePaths.login ||
        path == RoutePaths.register) {
      return;
    }

    router.go(shareRouteForUrl(url));
  }

  @override
  void dispose() {
    _subscription?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
