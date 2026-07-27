import 'package:shared_preferences/shared_preferences.dart';

/// Local auth data source (JWT access + refresh tokens).
abstract class AuthLocalDataSource {
  Future<String?> getAccessToken();

  Future<String?> getRefreshToken();

  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  });

  Future<void> clearSession();
}

class AuthLocalDataSourceImpl implements AuthLocalDataSource {
  AuthLocalDataSourceImpl({this._preferences});

  static const _accessKey = 'srs_access_token';
  static const _refreshKey = 'srs_refresh_token';

  SharedPreferences? _preferences;

  Future<SharedPreferences> _prefs() async {
    return _preferences ??= await SharedPreferences.getInstance();
  }

  @override
  Future<String?> getAccessToken() async {
    final prefs = await _prefs();
    return prefs.getString(_accessKey);
  }

  @override
  Future<String?> getRefreshToken() async {
    final prefs = await _prefs();
    return prefs.getString(_refreshKey);
  }

  @override
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  }) async {
    final prefs = await _prefs();
    await prefs.setString(_accessKey, accessToken);
    await prefs.setString(_refreshKey, refreshToken);
  }

  @override
  Future<void> clearSession() async {
    final prefs = await _prefs();
    await prefs.remove(_accessKey);
    await prefs.remove(_refreshKey);
  }
}
