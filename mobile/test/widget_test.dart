import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:mobile/app/app.dart';
import 'package:mobile/core/router/app_router.dart';
import 'package:mobile/core/router/route_paths.dart';
import 'package:mobile/core/constants/app_constants.dart';
import 'package:mobile/features/auth/presentation/pages/login_page.dart';
import 'package:mobile/features/auth/presentation/pages/register_page.dart';
import 'package:mobile/features/auth/presentation/pages/splash_page.dart';
import 'package:mobile/features/home/presentation/pages/home_page.dart';
import 'package:mobile/features/media/domain/repositories/media_repository.dart';
import 'package:mobile/features/media/presentation/providers/media_providers.dart';
import 'package:mobile/features/share/presentation/pages/share_page.dart';
import 'package:mobile/shared/models/media_item_preview.dart';
import 'package:mobile/shared/models/media_platform.dart';
import 'package:mobile/features/media/data/models/media_dto.dart';

class _FakeMediaRepository implements MediaRepository {
  @override
  Future<MediaItemPreview> createMedia({
    required String url,
    String? source,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<void> deleteMedia(String id) async {}

  @override
  Future<MediaItemPreview> getMedia(String id) {
    throw UnimplementedError();
  }

  @override
  Future<PlaybackDto> getPlayback(String id) {
    throw UnimplementedError();
  }

  @override
  Future<({List<MediaItemPreview> items, int totalCount})> listMedia({
    int page = 1,
    int pageSize = 20,
    MediaPlatform? platform,
    String? status,
  }) async {
    return (items: <MediaItemPreview>[], totalCount: 0);
  }

  @override
  Future<MediaItemPreview> retryMedia(String id) {
    throw UnimplementedError();
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  SharedPreferences.setMockInitialValues({});

  testWidgets('App boots to Splash and stays until Get Started', (tester) async {
    await tester.pumpWidget(
      const ProviderScope(
        child: SocialReelSaverApp(),
      ),
    );

    await tester.pump();
    expect(find.byType(SplashPage), findsOneWidget);

    await tester.pump(const Duration(seconds: 3));
    expect(find.byType(SplashPage), findsOneWidget);
    expect(find.byType(LoginPage), findsNothing);

    await tester.tap(find.text('Get started'));
    await tester.pumpAndSettle();
    expect(find.byType(LoginPage), findsOneWidget);
  });

  testWidgets('Login screen shows sign-in form', (tester) async {
    tester.view.physicalSize = const Size(800, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.reset);

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
    expect(find.text('Log in'), findsOneWidget);
    expect(find.text('Sign up'), findsOneWidget);
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
    expect(find.text('Name'), findsOneWidget);
    expect(find.text('Start saving reels in seconds'), findsOneWidget);
    expect(find.textContaining('Already have an account'), findsOneWidget);
    expect(find.text('Log in'), findsOneWidget);
    expect(find.text('Create account'), findsWidgets);
  });

  testWidgets('Home screen shows download dashboard', (tester) async {
    tester.view.physicalSize = const Size(800, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.reset);

    final router = createAppRouter(initialLocation: RoutePaths.home);
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          mediaRepositoryProvider.overrideWithValue(_FakeMediaRepository()),
        ],
        child: MaterialApp.router(
          theme: ThemeData(useMaterial3: true),
          routerConfig: router,
        ),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(HomePage), findsOneWidget);
    expect(find.text(AppConstants.appName), findsWidgets);
    expect(find.text('Your saved reels'), findsOneWidget);
    expect(find.text('Paste a link'), findsOneWidget);
    expect(find.text('Instagram'), findsOneWidget);
    expect(find.text('Facebook'), findsOneWidget);
    expect(find.text('Recent'), findsOneWidget);
    expect(find.text('See all'), findsOneWidget);
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
