/// Path templates aligned with SRS §7 screens.
abstract final class RoutePaths {
  /// Auth entry point. Splash decides Login vs Home in a later sprint.
  static const String splash = '/';
  static const String login = '/login';
  static const String register = '/register';
  static const String home = '/home';
  static const String library = '/library';
  static const String mediaDetail = '/media/:id';
  static const String mediaPlayer = '/media/:id/play';
  static const String settings = '/settings';
  static const String share = '/share';
  static const String notifications = '/notifications';

  static String mediaDetailPath(String id) => '/media/$id';
  static String mediaPlayerPath(String id) => '/media/$id/play';
}
