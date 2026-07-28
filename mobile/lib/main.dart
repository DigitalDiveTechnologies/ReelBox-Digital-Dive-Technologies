import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app/app.dart';
import 'app/bootstrap.dart';
import 'core/constants/app_constants.dart';
import 'core/router/app_router_provider.dart';
import 'features/share/data/datasources/android_share_intent_registrar.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
    AndroidShareIntentRegistrar.instance.ensureInitialized();
  }

  FlutterError.onError = (FlutterErrorDetails details) {
    FlutterError.presentError(details);
    debugPrint('FLUTTER_ERROR: ${details.exceptionAsString()}');
  };

  final boot = await bootstrap();

  runApp(
    ProviderScope(
      overrides: [
        initialRouteProvider.overrideWithValue(boot.initialLocation),
      ],
      // ReelBox — MaterialApp title comes from [AppConstants.appName].
      child: const SocialReelSaverApp(),
    ),
  );

  assert(() {
    debugPrint('Starting ${AppConstants.appName}');
    return true;
  }());
}
