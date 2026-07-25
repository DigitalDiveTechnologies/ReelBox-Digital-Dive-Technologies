/// Local auth data source placeholder (tokens / session cache).
///
/// TODO: Persist access/refresh tokens via secure storage when auth is wired.
abstract class AuthLocalDataSource {
  /// TODO: Read access token from secure storage.
  Future<String?> getAccessToken();

  /// TODO: Read refresh token from secure storage.
  Future<String?> getRefreshToken();

  /// TODO: Persist tokens returned by backend auth API.
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  });

  /// TODO: Clear all locally stored auth credentials.
  Future<void> clearSession();
}

class AuthLocalDataSourceImpl implements AuthLocalDataSource {
  const AuthLocalDataSourceImpl();

  @override
  Future<String?> getAccessToken() async {
    // TODO: Read from secure storage after backend auth is integrated.
    return null;
  }

  @override
  Future<String?> getRefreshToken() async {
    // TODO: Read from secure storage after backend auth is integrated.
    return null;
  }

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    // TODO: Write tokens from backend auth API into secure storage.
  }

  @override
  Future<void> clearSession() async {
    // TODO: Clear secure storage session after logout API succeeds.
  }
}
