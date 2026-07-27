import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/pages/login_page.dart';
import '../../features/auth/presentation/pages/register_page.dart';
import '../../features/auth/presentation/pages/splash_page.dart';
import '../../features/home/presentation/pages/home_page.dart';
import '../../features/library/presentation/pages/library_page.dart';
import '../../features/media_detail/presentation/pages/media_detail_page.dart';
import '../../features/media_detail/presentation/pages/media_player_page.dart';
import '../../features/settings/presentation/pages/settings_page.dart';
import '../../features/share/presentation/pages/share_page.dart';
import '../../shared/widgets/main_shell_scaffold.dart';
import 'route_names.dart';
import 'route_paths.dart';

/// Application router.
///
/// Splash is the entry point so auth can gate navigation later.
///
/// TODO: Add redirect/refreshListenable once backend auth session APIs exist.
GoRouter createAppRouter({String initialLocation = RoutePaths.splash}) {
  return GoRouter(
    initialLocation: initialLocation,
    errorBuilder: (BuildContext context, GoRouterState state) {
      return Scaffold(
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Text(
              'Could not open this screen.\n${state.error ?? state.uri}',
              textAlign: TextAlign.center,
            ),
          ),
        ),
      );
    },
    routes: <RouteBase>[
      GoRoute(
        path: RoutePaths.splash,
        name: RouteNames.splash,
        builder: (BuildContext context, GoRouterState state) {
          return const SplashPage();
        },
      ),
      GoRoute(
        path: RoutePaths.login,
        name: RouteNames.login,
        builder: (BuildContext context, GoRouterState state) {
          return const LoginPage();
        },
      ),
      GoRoute(
        path: RoutePaths.register,
        name: RouteNames.register,
        builder: (BuildContext context, GoRouterState state) {
          return const RegisterPage();
        },
      ),
      StatefulShellRoute.indexedStack(
        builder: (
          BuildContext context,
          GoRouterState state,
          StatefulNavigationShell navigationShell,
        ) {
          return MainShellScaffold(navigationShell: navigationShell);
        },
        branches: <StatefulShellBranch>[
          StatefulShellBranch(
            routes: <RouteBase>[
              GoRoute(
                path: RoutePaths.home,
                name: RouteNames.home,
                builder: (BuildContext context, GoRouterState state) {
                  return const HomePage();
                },
              ),
            ],
          ),
          StatefulShellBranch(
            routes: <RouteBase>[
              GoRoute(
                path: RoutePaths.library,
                name: RouteNames.library,
                builder: (BuildContext context, GoRouterState state) {
                  return const LibraryPage();
                },
              ),
            ],
          ),
          StatefulShellBranch(
            routes: <RouteBase>[
              GoRoute(
                path: RoutePaths.settings,
                name: RouteNames.settings,
                builder: (BuildContext context, GoRouterState state) {
                  return const SettingsPage();
                },
              ),
            ],
          ),
        ],
      ),
      GoRoute(
        path: RoutePaths.mediaDetail,
        name: RouteNames.mediaDetail,
        builder: (BuildContext context, GoRouterState state) {
          final id = state.pathParameters['id'] ?? '';
          return MediaDetailPage(mediaId: id);
        },
        routes: <RouteBase>[
          GoRoute(
            path: 'play',
            name: RouteNames.mediaPlayer,
            builder: (BuildContext context, GoRouterState state) {
              final id = state.pathParameters['id'] ?? '';
              return MediaPlayerPage(mediaId: id);
            },
          ),
        ],
      ),
      GoRoute(
        path: RoutePaths.share,
        name: RouteNames.share,
        builder: (BuildContext context, GoRouterState state) {
          // Deep-link / Android Share Intent entry: /share?url=
          final url = state.uri.queryParameters['url'];
          return SharePage(sharedUrl: url);
        },
      ),
    ],
  );
}
