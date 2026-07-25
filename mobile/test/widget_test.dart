import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:mobile/app/app.dart';
import 'package:mobile/core/router/app_router.dart';
import 'package:mobile/core/router/route_paths.dart';
import 'package:mobile/core/constants/app_constants.dart';
import 'package:mobile/features/auth/presentation/pages/login_page.dart';
import 'package:mobile/features/auth/presentation/pages/register_page.dart';
import 'package:mobile/features/auth/presentation/pages/splash_page.dart';
import 'package:mobile/features/home/presentation/pages/home_page.dart';
import 'package:mobile/features/share/presentation/pages/share_page.dart';

void main() {
  testWidgets('App boots to Splash then routes to Login when unsigned', (tester) async {
    await tester.pumpWidget(
      const ProviderScope(
        child: SocialReelSaverApp(),
      ),
    );

    // First frame is Splash (auth entry).
    await tester.pump();
    expect(find.byType(SplashPage), findsOneWidget);

    // After brand animation + session placeholder, unsigned users land on Login.
    await tester.pumpAndSettle(const Duration(seconds: 3));
    expect(find.byType(LoginPage), findsOneWidget);
  });

  testWidgets('Login screen shows sign-in form', (tester) async {
    final router = createAppRouter(initialLocation: RoutePaths.login);
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp.router(
          theme: ThemeData(useMaterial3: true),
          routerConfig: router,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(LoginPage), findsOneWidget);
    expect(find.text('Welcome back'), findsOneWidget);
    expect(find.text('Sign in'), findsOneWidget);
    expect(find.text('Create account'), findsOneWidget);
  });

  testWidgets('Register screen shows create-account form', (tester) async {
    tester.view.physicalSize = const Size(800, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.reset);

    final router = createAppRouter(initialLocation: RoutePaths.register);
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp.router(
          theme: ThemeData(useMaterial3: true),
          routerConfig: router,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(RegisterPage), findsOneWidget);
    expect(find.text('Confirm password'), findsOneWidget);
    expect(find.textContaining('Already have an account'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Create account'), findsOneWidget);
  });

  testWidgets('Home screen shows download dashboard', (tester) async {
    tester.view.physicalSize = const Size(800, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.reset);

    final router = createAppRouter(initialLocation: RoutePaths.home);
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp.router(
          theme: ThemeData(useMaterial3: true),
          routerConfig: router,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(HomePage), findsOneWidget);
    expect(find.text(AppConstants.appName), findsWidgets);
    expect(find.text('Your downloads'), findsOneWidget);
    expect(find.text('Paste a URL'), findsOneWidget);
    expect(find.text('Download status'), findsOneWidget);
    expect(find.text('Recent downloads'), findsOneWidget);

    await tester.tap(find.byTooltip('Show empty dashboard'));
    await tester.pumpAndSettle();
    expect(find.text('No downloads yet'), findsOneWidget);
    expect(find.textContaining('choose Social'), findsOneWidget);
  });

  testWidgets('Share route shows received URL from query', (WidgetTester tester) async {
    final router = createAppRouter(initialLocation: RoutePaths.share);
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp.router(
          routerConfig: router,
        ),
      ),
    );

    router.go(
      '${RoutePaths.share}?url=${Uri.encodeComponent('https://www.instagram.com/reel/ABC123/')}',
    );
    await tester.pumpAndSettle();

    expect(find.byType(SharePage), findsOneWidget);
    expect(find.text('Received URL'), findsOneWidget);
    expect(
      find.text('https://www.instagram.com/reel/ABC123/'),
      findsOneWidget,
    );
  });

  testWidgets('Share route shows empty state when url is missing', (WidgetTester tester) async {
    final router = createAppRouter(initialLocation: RoutePaths.share);
    await tester.pumpWidget(
      ProviderScope(
        child: MaterialApp.router(
          routerConfig: router,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(SharePage), findsOneWidget);
    expect(find.text('No shared URL received.'), findsOneWidget);
  });
}
