import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import '../models/auth_session_model.dart';
import '../models/auth_user_model.dart';

/// Remote auth data source (SRS §22 JWT endpoints).
abstract class AuthRemoteDataSource {
  Future<AuthSessionModel> login({
    required String email,
    required String password,
  });

  Future<AuthSessionModel> register({
    required String email,
    required String password,
  });

  Future<void> logout();

  Future<AuthUserModel?> getCurrentUser();

  Future<AuthSessionModel> refreshToken({required String refreshToken});
}

class AuthRemoteDataSourceImpl implements AuthRemoteDataSource {
  AuthRemoteDataSourceImpl(this._api);

  final ApiClient _api;

  @override
  Future<AuthSessionModel> login({
    required String email,
    required String password,
  }) async {
    final json = await _api.postJson(
      ApiEndpoints.authLogin,
      body: {'email': email, 'password': password},
      authenticated: false,
    );
    return AuthSessionModel.fromJson(json);
  }

  @override
  Future<AuthSessionModel> register({
    required String email,
    required String password,
  }) async {
    final json = await _api.postJson(
      ApiEndpoints.authRegister,
      body: {'email': email, 'password': password},
      authenticated: false,
    );
    return AuthSessionModel.fromJson(json);
  }

  @override
  Future<void> logout() async {
    await _api.postJson(ApiEndpoints.authLogout, body: <String, dynamic>{});
  }

  @override
  Future<AuthUserModel?> getCurrentUser() async {
    final json = await _api.getJson(ApiEndpoints.authMe);
    return AuthUserModel.fromJson(json);
  }

  @override
  Future<AuthSessionModel> refreshToken({required String refreshToken}) async {
    final json = await _api.postJson(
      ApiEndpoints.authRefresh,
      body: {'refreshToken': refreshToken},
      authenticated: false,
    );
    return AuthSessionModel.fromJson(json);
  }
}
