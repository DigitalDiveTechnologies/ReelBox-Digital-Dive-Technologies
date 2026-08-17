import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../core/router/route_paths.dart';
import '../features/auth/data/datasources/auth_local_datasource.dart';

/// Startup result used to pick the first route without flashing Splash when signed in.
class BootstrapResult {
  const BootstrapResult({required this.initialLocation});

  final String initialLocation;
}

/// Async startup: resolve session so authenticated users open Home directly.
Future<BootstrapResult> bootstrap() async {
  try {
    final prefs = await SharedPreferences.getInstance();
    final local = AuthLocalDataSourceImpl(preferences: prefs);
    final access = await local.getAccessToken();
    final refresh = await local.getRefreshToken();
    final accessPresent = access != null && access.trim().isNotEmpty;
    final refreshPresent = refresh != null && refresh.trim().isNotEmpty;
    debugPrint('[AUTH_DEBUG] startup: accessTokenPresent=$accessPresent');
    debugPrint('[AUTH_DEBUG] startup: refreshTokenPresent=$refreshPresent');
    final signedIn = access != null && access.trim().isNotEmpty;
    debugPrint('[AUTH_DEBUG] startup: restoredSession=$signedIn');
    if (!signedIn) {
      debugPrint(
        '[AUTH_DEBUG] startup: entering Splash because session missing',
      );
    }
    return BootstrapResult(
      initialLocation: signedIn ? RoutePaths.home : RoutePaths.splash,
    );
  } catch (error) {
    debugPrint('[AUTH_DEBUG] startup: bootstrap exception type=${error.runtimeType}');
    debugPrint(
      '[AUTH_DEBUG] startup: entering Splash because session missing',
    );
    return const BootstrapResult(initialLocation: RoutePaths.splash);
  }
}
