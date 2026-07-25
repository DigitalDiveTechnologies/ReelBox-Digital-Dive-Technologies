import '../../domain/entities/auth_user.dart';
import '../../domain/repositories/auth_repository.dart';

/// Presentation-side auth controller placeholder.
///
/// Holds future session actions without implementing them yet.
class AuthController {
  AuthController(this._repository);

  final AuthRepository _repository;

  /// TODO: Load session via backend / local token APIs for Splash navigation.
  Future<AuthUser?> loadCurrentUser() {
    return _repository.getCurrentUser();
  }

  /// TODO: Submit credentials to backend login API.
  Future<AuthUser> login({
    required String email,
    required String password,
  }) {
    return _repository.login(email: email, password: password);
  }

  /// TODO: Call backend logout API and clear local session.
  Future<void> logout() {
    return _repository.logout();
  }

  /// TODO: Resolve auth status for route guards / Splash flow.
  Future<bool> isAuthenticated() {
    return _repository.isAuthenticated();
  }
}
