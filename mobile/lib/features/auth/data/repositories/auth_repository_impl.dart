import '../../../../core/errors/app_exception.dart';
import '../../domain/entities/auth_user.dart';
import '../../domain/repositories/auth_repository.dart';
import '../datasources/auth_local_datasource.dart';
import '../datasources/auth_remote_datasource.dart';

/// Empty [AuthRepository] implementation.
///
/// TODO: Orchestrate remote auth APIs + local token storage in a later sprint.
class AuthRepositoryImpl implements AuthRepository {
  const AuthRepositoryImpl({
    required this.remoteDataSource,
    required this.localDataSource,
  });

  // Kept for future wiring; unused until backend auth is implemented.
  // ignore: unused_field
  final AuthRemoteDataSource remoteDataSource;
  // ignore: unused_field
  final AuthLocalDataSource localDataSource;

  @override
  Future<AuthUser?> getCurrentUser() async {
    // TODO: Use local tokens + optional backend /me API; map DTO → [AuthUser].
    return null;
  }

  @override
  Future<AuthUser> login({
    required String email,
    required String password,
  }) async {
    // TODO: Call remote login API, persist tokens locally, return domain user.
    // No fake session — surface a catchable placeholder failure instead of crashing.
    throw const AppException(
      message: 'Login is not available yet.',
      code: 'AUTH_NOT_IMPLEMENTED',
    );
  }

  @override
  Future<void> logout() async {
    // TODO: Call remote logout API (if required) and clear local session.
  }

  @override
  Future<bool> isAuthenticated() async {
    // TODO: Inspect local tokens / expiry; refresh via backend when needed.
    return false;
  }
}
