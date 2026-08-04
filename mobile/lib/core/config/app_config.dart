import 'env.dart';

/// Compile-time configuration via `--dart-define`.
///
/// Example:
/// ```bash
/// flutter run \
///   --dart-define=ENV=dev \
///   --dart-define=API_BASE_URL=http://192.168.100.8:5080/api
/// ```
class AppConfig {
  const AppConfig._();

  static const String _envRaw = String.fromEnvironment(
    'ENV',
    defaultValue: 'dev',
  );

  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://192.168.100.8:5080/api',
  );

  static Env get env => Env.fromString(_envRaw);

  static bool get isDev => env == Env.dev;
  static bool get isStaging => env == Env.staging;
  static bool get isProd => env == Env.prod;
}
