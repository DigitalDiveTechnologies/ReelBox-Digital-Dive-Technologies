import '../../domain/entities/auth_user.dart';
import '../../domain/repositories/auth_repository.dart';

/// Presentation-side auth controller.
class AuthController {
  AuthController(this._repository);

  final AuthRepository _repository;

  Future<AuthUser?> loadCurrentUser() {
    return _repository.getCurrentUser();
  }

  Future<AuthUser> login({
    required String email,
    required String password,
  }) {
    return _repository.login(email: email, password: password);
  }

  Future<String> register({
    required String email,
    required String password,
  }) {
    return _repository.register(email: email, password: password);
  }

  Future<AuthUser> verifySignupOtp({
    required String email,
    required String otp,
  }) {
    return _repository.verifySignupOtp(email: email, otp: otp);
  }

  Future<void> logout() {
    return _repository.logout();
  }

  Future<bool> isAuthenticated() {
    return _repository.isAuthenticated();
  }
}
