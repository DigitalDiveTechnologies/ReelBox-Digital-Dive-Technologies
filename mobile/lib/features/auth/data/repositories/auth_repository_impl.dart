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
      return model?.toDomain();
    } catch (_) {
      final refreshed = await _refreshIfPossible();
      if (!refreshed) {
        return null;
      }
      final model = await remoteDataSource.getCurrentUser();
      return model?.toDomain();
    }
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
    return session.user.toDomain();
  }

  @override
  Future<AuthUser> register({
    required String email,
    required String password,
  }) async {
    final session = await remoteDataSource.register(
      email: email.trim(),
      password: password,
    );
    await localDataSource.saveTokens(
      accessToken: session.tokens.accessToken,
      refreshToken: session.tokens.refreshToken,
    );
    return session.user.toDomain();
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
