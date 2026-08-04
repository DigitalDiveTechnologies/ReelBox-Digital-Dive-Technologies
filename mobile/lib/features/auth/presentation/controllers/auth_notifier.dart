import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../domain/entities/auth_user.dart';
import '../../domain/repositories/auth_repository.dart';
import '../providers/auth_providers.dart';

/// Session notifier — keeps [AuthState] in sync after login/register/logout.
class AuthNotifier extends AsyncNotifier<AuthState> {
  AuthRepository get _repository => ref.read(authRepositoryProvider);

  @override
  Future<AuthState> build() async {
    final user = await _repository.getCurrentUser();
    return AuthState(user: user);
  }

  Future<AuthUser> login({
    required String email,
    required String password,
  }) async {
    state = const AsyncLoading();
    try {
      final user = await _repository.login(email: email, password: password);
      state = AsyncData(AuthState(user: user));
      return user;
    } catch (error, stackTrace) {
      state = AsyncError(error, stackTrace);
      rethrow;
    }
  }

  /// Starts signup (sends OTP). Does not create a session yet.
  Future<String> register({
    required String email,
    required String password,
  }) {
    return _repository.register(email: email, password: password);
  }

  Future<AuthUser> verifySignupOtp({
    required String email,
    required String otp,
  }) async {
    state = const AsyncLoading();
    try {
      final user = await _repository.verifySignupOtp(email: email, otp: otp);
      state = AsyncData(AuthState(user: user));
      return user;
    } catch (error, stackTrace) {
      state = AsyncError(error, stackTrace);
      rethrow;
    }
  }

  Future<void> logout() async {
    state = const AsyncLoading();
    try {
      await _repository.logout();
      state = const AsyncData(AuthState());
    } catch (error, stackTrace) {
      state = AsyncError(error, stackTrace);
      rethrow;
    }
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = AsyncData(AuthState(user: await _repository.getCurrentUser()));
  }
}

final authNotifierProvider =
    AsyncNotifierProvider<AuthNotifier, AuthState>(AuthNotifier.new);
