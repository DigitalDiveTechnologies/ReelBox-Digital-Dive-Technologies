/// Authenticated user representation for the domain layer.
///
/// Fields will expand when backend auth contracts are finalized.
class AuthUser {
  const AuthUser({
    required this.id,
    this.email,
  });

  final String id;
  final String? email;
}
