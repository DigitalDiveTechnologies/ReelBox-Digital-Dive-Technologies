import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'app_router.dart';
import 'route_paths.dart';

/// Cold-start initial route: Home when signed in, Splash when signed out.
final initialRouteProvider = Provider<String>((ref) => RoutePaths.splash);

/// Application [GoRouter] instance.
final appRouterProvider = Provider<GoRouter>((ref) {
  final initialLocation = ref.watch(initialRouteProvider);
  final router = createAppRouter(initialLocation: initialLocation);
  ref.onDispose(router.dispose);
  return router;
});
