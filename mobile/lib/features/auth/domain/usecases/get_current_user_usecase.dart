import '../entities/auth_user.dart';
import '../repositories/auth_repository.dart';

/// Placeholder use case: resolve the current authenticated user.
class GetCurrentUserUseCase {
  const GetCurrentUserUseCase(this._repository);

  final AuthRepository _repository;

  /// TODO: Will depend on backend session validation / token refresh APIs.
  Future<AuthUser?> call() {
    return _repository.getCurrentUser();
  }
}
