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
    final signedIn = access != null && access.trim().isNotEmpty;
    return BootstrapResult(
      initialLocation: signedIn ? RoutePaths.home : RoutePaths.splash,
    );
  } catch (_) {
    return const BootstrapResult(initialLocation: RoutePaths.splash);
  }
}
