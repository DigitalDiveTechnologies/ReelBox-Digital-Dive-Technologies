import '../entities/auth_user.dart';

/// Contract for authentication operations.
///
/// TODO: Implementations will call backend auth APIs and secure storage.
abstract class AuthRepository {
  /// Returns the current session user, or `null` if none.
  ///
  /// TODO: Read persisted tokens and optionally validate with backend API.
  Future<AuthUser?> getCurrentUser();

  /// Signs the user in.
  ///
  /// TODO: POST credentials to backend auth API; persist tokens securely.
  Future<AuthUser> login({
    required String email,
    required String password,
  });

  /// Signs the user out.
  ///
  /// TODO: Revoke refresh token via backend API; clear local session.
  Future<void> logout();

  /// Whether a usable local session exists.
  ///
  /// TODO: Inspect secure storage / token expiry without network when possible.
  Future<bool> isAuthenticated();
}
