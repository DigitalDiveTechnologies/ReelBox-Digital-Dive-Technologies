import '../repositories/auth_repository.dart';

/// Placeholder use case: sign out the current user.
class LogoutUseCase {
  const LogoutUseCase(this._repository);

  final AuthRepository _repository;

  /// TODO: Will depend on backend logout / token-revoke API.
  Future<void> call() {
    return _repository.logout();
  }
}
