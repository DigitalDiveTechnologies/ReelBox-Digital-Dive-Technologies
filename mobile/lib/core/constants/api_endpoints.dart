/// REST paths aligned with SRS §9 (Proposed Backend API).
class ApiEndpoints {
  const ApiEndpoints._();

  static const String apiV1 = '/api/v1';

  static const String media = '$apiV1/media';

  static String mediaById(String id) => '$media/$id';

  static String mediaRetry(String id) => '$media/$id/retry';

  static String mediaPlayback(String id) => '$media/$id/playback';
}
