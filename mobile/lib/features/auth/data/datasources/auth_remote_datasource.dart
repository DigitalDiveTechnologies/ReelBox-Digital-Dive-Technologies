import '../../../../core/errors/app_exception.dart';
import '../models/auth_user_model.dart';

/// Remote auth data source placeholder.
///
/// TODO: Perform HTTP calls to backend auth endpoints (login, logout, me, refresh).
abstract class AuthRemoteDataSource {
  /// TODO: POST /api/v1/auth/login (or equivalent) when backend is ready.
  Future<AuthUserModel> login({
    required String email,
    required String password,
  });

  /// TODO: POST /api/v1/auth/logout (or equivalent) when backend is ready.
  Future<void> logout();

  /// TODO: GET /api/v1/auth/me (or equivalent) when backend is ready.
  Future<AuthUserModel?> getCurrentUser();

  /// TODO: POST /api/v1/auth/refresh when JWT refresh is implemented.
  Future<void> refreshToken();
}

class AuthRemoteDataSourceImpl implements AuthRemoteDataSource {
  const AuthRemoteDataSourceImpl();

  @override
  Future<AuthUserModel> login({
    required String email,
    required String password,
  }) async {
    // TODO: Call backend login API. Do not implement fake authentication.
    throw const AppException(
      message: 'Login API is not wired yet.',
      code: 'AUTH_NOT_IMPLEMENTED',
    );
  }

  @override
  Future<void> logout() async {
    // TODO: Call backend logout / token-revoke API.
  }

  @override
  Future<AuthUserModel?> getCurrentUser() async {
    // TODO: Call backend current-user API.
    return null;
  }

  @override
  Future<void> refreshToken() async {
    // TODO: Call backend token refresh API.
  }
}
