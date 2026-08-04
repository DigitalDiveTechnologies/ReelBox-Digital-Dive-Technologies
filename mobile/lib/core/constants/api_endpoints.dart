/// REST paths aligned with SRS §9 and JWT auth (§22).
class ApiEndpoints {
  const ApiEndpoints._();

  static const String apiV1 = '/api/v1';

  static const String authRegister = '$apiV1/auth/register';
  static const String authLogin = '$apiV1/auth/login';
  static const String authRefresh = '$apiV1/auth/refresh';
  static const String authLogout = '$apiV1/auth/logout';
  static const String authMe = '$apiV1/auth/me';
  static const String authForgotPassword = '$apiV1/auth/forgot-password';
  static const String authResetPassword = '$apiV1/auth/reset-password';
  static const String authVerifyEmail = '$apiV1/auth/verify-email';
  /// Alias kept for older clients / docs; same handler as [authVerifyEmail].
  static const String authVerifyOtp = '$apiV1/auth/verify-otp';
  static const String authResendSignupOtp = '$apiV1/auth/resend-signup-otp';

  static const String media = '$apiV1/media';

  static const String notifications = '$apiV1/notifications';

  static String mediaById(String id) => '$media/$id';

  static String mediaRetry(String id) => '$media/$id/retry';

  static String mediaPlayback(String id) => '$media/$id/playback';
}
