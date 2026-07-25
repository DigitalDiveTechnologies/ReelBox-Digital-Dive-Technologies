/// Abstract HTTP client contract.
///
/// Concrete implementation (e.g. Dio) will be added when networking is wired.
abstract class ApiClient {
  Future<T> get<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
  });

  Future<T> post<T>(
    String path, {
    Object? data,
    Map<String, dynamic>? queryParameters,
  });

  Future<T> delete<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
  });
}
