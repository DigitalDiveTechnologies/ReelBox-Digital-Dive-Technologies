import '../entities/auth_user.dart';
import '../repositories/auth_repository.dart';

/// Placeholder use case: sign in with credentials.
class LoginUseCase {
  const LoginUseCase(this._repository);

  final AuthRepository _repository;

  /// TODO: Will depend on backend login API (JWT + refresh token response).
  Future<AuthUser> call({
    required String email,
    required String password,
  }) {
    return _repository.login(email: email, password: password);
  }
}
