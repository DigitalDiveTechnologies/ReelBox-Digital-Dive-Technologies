import 'package:shared_preferences/shared_preferences.dart';

/// Local auth data source (JWT access + refresh tokens + cached user profile).
abstract class AuthLocalDataSource {
  Future<String?> getAccessToken();

  Future<String?> getRefreshToken();

  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
  });

  Future<({String id, String? email})?> getCachedUser();

  Future<void> saveCachedUser({
    required String id,
    String? email,
  });

  Future<void> clearSession();
}

class AuthLocalDataSourceImpl implements AuthLocalDataSource {
  AuthLocalDataSourceImpl({this._preferences});

  static const _accessKey = 'srs_access_token';
  static const _refreshKey = 'srs_refresh_token';
  static const _userIdKey = 'srs_user_id';
  static const _userEmailKey = 'srs_user_email';

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
  Future<({String id, String? email})?> getCachedUser() async {
    final prefs = await _prefs();
    final id = prefs.getString(_userIdKey);
    if (id == null || id.isEmpty) return null;
    return (id: id, email: prefs.getString(_userEmailKey));
  }

  @override
  Future<void> saveCachedUser({
    required String id,
    String? email,
  }) async {
    final prefs = await _prefs();
    await prefs.setString(_userIdKey, id);
    if (email == null || email.isEmpty) {
      await prefs.remove(_userEmailKey);
    } else {
      await prefs.setString(_userEmailKey, email);
    }
  }

  @override
  Future<void> clearSession() async {
    final prefs = await _prefs();
    await prefs.remove(_accessKey);
    await prefs.remove(_refreshKey);
    await prefs.remove(_userIdKey);
    await prefs.remove(_userEmailKey);
  }
}
