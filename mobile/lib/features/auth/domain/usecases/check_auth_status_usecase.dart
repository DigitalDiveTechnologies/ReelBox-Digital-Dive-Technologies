import '../repositories/auth_repository.dart';

/// Placeholder use case: check whether the user has an active session.
class CheckAuthStatusUseCase {
  const CheckAuthStatusUseCase(this._repository);

  final AuthRepository _repository;

  /// TODO: Will depend on local token storage and optional backend introspect API.
  Future<bool> call() {
    return _repository.isAuthenticated();
  }
}
