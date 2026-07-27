import '../../domain/entities/auth_user.dart';

/// Contract for authentication operations (JWT + refresh).
abstract class AuthRepository {
  Future<AuthUser?> getCurrentUser();

  Future<AuthUser> login({
    required String email,
    required String password,
  });

  Future<AuthUser> register({
    required String email,
    required String password,
  });

  Future<void> logout();

  Future<bool> isAuthenticated();
}
