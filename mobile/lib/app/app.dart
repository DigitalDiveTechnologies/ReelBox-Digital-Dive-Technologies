import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/constants/app_constants.dart';
import '../core/router/app_router_provider.dart';
import '../core/theme/app_theme.dart';
import '../features/media/presentation/providers/gallery_export_providers.dart';
import '../features/share/presentation/widgets/android_share_intent_listener.dart';

class SocialReelSaverApp extends ConsumerWidget {
  const SocialReelSaverApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // App-scoped Task 1 coordinator (survives Media Detail navigation).
    ref.watch(galleryExportBootstrapProvider);

    final router = ref.watch(appRouterProvider);

    return AndroidShareIntentListener(
      child: MaterialApp.router(
        title: AppConstants.appName,
        debugShowCheckedModeBanner: false,
        theme: AppTheme.light,
        darkTheme: AppTheme.dark,
        themeMode: ThemeMode.system,
        routerConfig: router,
      ),
    );
  }
}
