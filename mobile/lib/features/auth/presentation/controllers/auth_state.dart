import '../../domain/entities/auth_user.dart';

/// Immutable auth UI state for Settings and session-aware screens.
class AuthState {
  const AuthState({
    this.user,
    this.isLoading = false,
  });

  final AuthUser? user;
  final bool isLoading;

  bool get isAuthenticated {
    final email = user?.email?.trim();
    return user != null &&
        ((email != null && email.isNotEmpty) || user!.id.trim().isNotEmpty);
  }

  String get displayName {
    final email = user?.email?.trim();
    if (email == null || email.isEmpty) return 'Signed in';
    if (email.contains('@')) return email.split('@').first;
    return email;
  }

  String get displayEmail {
    final email = user?.email?.trim();
    if (email == null || email.isEmpty) return 'Account active';
    return email;
  }

  AuthState copyWith({
    AuthUser? user,
    bool? isLoading,
    bool clearUser = false,
  }) {
    return AuthState(
      user: clearUser ? null : (user ?? this.user),
      isLoading: isLoading ?? this.isLoading,
    );
  }
}
