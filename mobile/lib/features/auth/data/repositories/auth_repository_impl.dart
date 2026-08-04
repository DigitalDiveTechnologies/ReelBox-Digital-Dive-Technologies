import '../../domain/entities/auth_user.dart';
import '../../domain/repositories/auth_repository.dart';
import '../datasources/auth_local_datasource.dart';
import '../datasources/auth_remote_datasource.dart';
import '../models/auth_user_model.dart';

class AuthRepositoryImpl implements AuthRepository {
  const AuthRepositoryImpl({
    required this.remoteDataSource,
    required this.localDataSource,
  });

  final AuthRemoteDataSource remoteDataSource;
  final AuthLocalDataSource localDataSource;

  @override
  Future<AuthUser?> getCurrentUser() async {
    final token = await localDataSource.getAccessToken();
    if (token == null || token.isEmpty) {
      return null;
    }

    try {
      final model = await remoteDataSource.getCurrentUser();
      final user = model?.toDomain();
      if (user != null) {
        await localDataSource.saveCachedUser(id: user.id, email: user.email);
        return user;
      }
    } catch (_) {
      final refreshed = await _refreshIfPossible();
      if (refreshed) {
        try {
          final model = await remoteDataSource.getCurrentUser();
          final user = model?.toDomain();
          if (user != null) {
            await localDataSource.saveCachedUser(id: user.id, email: user.email);
            return user;
          }
        } catch (_) {
          // Fall through to local cache.
        }
      }
    }

    final cached = await localDataSource.getCachedUser();
    if (cached != null) {
      return AuthUser(id: cached.id, email: cached.email);
    }

    return null;
  }

  @override
  Future<AuthUser> login({
    required String email,
    required String password,
  }) async {
    final session = await remoteDataSource.login(
      email: email.trim(),
      password: password,
    );
    await localDataSource.saveTokens(
      accessToken: session.tokens.accessToken,
      refreshToken: session.tokens.refreshToken,
    );
    final user = session.user.toDomain();
    await localDataSource.saveCachedUser(id: user.id, email: user.email);
    return user;
  }

  @override
  Future<String> register({
    required String email,
    required String password,
  }) {
    return remoteDataSource.register(
      email: email.trim(),
      password: password,
    );
  }

  @override
  Future<AuthUser> verifySignupOtp({
    required String email,
    required String otp,
  }) async {
    final session = await remoteDataSource.verifySignupOtp(
      email: email.trim(),
      otp: otp.trim(),
    );
    await localDataSource.saveTokens(
      accessToken: session.tokens.accessToken,
      refreshToken: session.tokens.refreshToken,
    );
    final user = session.user.toDomain();
    await localDataSource.saveCachedUser(id: user.id, email: user.email);
    return user;
  }

  @override
  Future<String> resendSignupOtp({required String email}) {
    return remoteDataSource.resendSignupOtp(email: email.trim());
  }

  @override
  Future<void> logout() async {
    try {
      await remoteDataSource.logout();
    } catch (_) {
      // Always clear local session even if revoke fails.
    }
    await localDataSource.clearSession();
  }

  @override
  Future<bool> isAuthenticated() async {
    final access = await localDataSource.getAccessToken();
    if (access != null && access.isNotEmpty) {
      return true;
    }
    return _refreshIfPossible();
  }

  @override
  Future<String> forgotPassword({required String email}) {
    return remoteDataSource.forgotPassword(email: email.trim());
  }

  @override
  Future<String> resetPassword({
    required String email,
    required String otp,
    required String newPassword,
  }) {
    return remoteDataSource.resetPassword(
      email: email.trim(),
      otp: otp.trim(),
      newPassword: newPassword,
    );
  }

  Future<bool> _refreshIfPossible() async {
    final refresh = await localDataSource.getRefreshToken();
    if (refresh == null || refresh.isEmpty) {
      await localDataSource.clearSession();
      return false;
    }

    try {
      final session = await remoteDataSource.refreshToken(refreshToken: refresh);
      await localDataSource.saveTokens(
        accessToken: session.tokens.accessToken,
        refreshToken: session.tokens.refreshToken,
      );
      final user = session.user.toDomain();
      await localDataSource.saveCachedUser(id: user.id, email: user.email);
      return true;
    } catch (_) {
      await localDataSource.clearSession();
      return false;
    }
  }
}

extension on AuthUserModel {
  AuthUser toDomain() => AuthUser(id: id, email: email);
}
