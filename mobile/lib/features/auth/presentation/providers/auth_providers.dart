import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_client_provider.dart';
import '../../data/datasources/auth_remote_datasource.dart';
import '../../data/repositories/auth_repository_impl.dart';
import '../../domain/entities/auth_user.dart';
import '../../domain/repositories/auth_repository.dart';
import '../../domain/usecases/check_auth_status_usecase.dart';
import '../../domain/usecases/forgot_password_usecase.dart';
import '../../domain/usecases/get_current_user_usecase.dart';
import '../../domain/usecases/login_usecase.dart';
import '../../domain/usecases/logout_usecase.dart';
import '../../domain/usecases/register_usecase.dart';
import '../../domain/usecases/reset_password_usecase.dart';
import '../controllers/auth_controller.dart';
import '../controllers/auth_notifier.dart';

export '../../../../core/network/api_client_provider.dart'
    show apiClientProvider, authLocalDataSourceProvider;
export '../controllers/auth_notifier.dart' show authNotifierProvider, AuthNotifier;
export '../controllers/auth_state.dart' show AuthState;

final authRemoteDataSourceProvider = Provider<AuthRemoteDataSource>((ref) {
  return AuthRemoteDataSourceImpl(ref.watch(apiClientProvider));
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepositoryImpl(
    remoteDataSource: ref.watch(authRemoteDataSourceProvider),
    localDataSource: ref.watch(authLocalDataSourceProvider),
  );
});

final getCurrentUserUseCaseProvider = Provider<GetCurrentUserUseCase>((ref) {
  return GetCurrentUserUseCase(ref.watch(authRepositoryProvider));
});

final loginUseCaseProvider = Provider<LoginUseCase>((ref) {
  return LoginUseCase(ref.watch(authRepositoryProvider));
});

final registerUseCaseProvider = Provider<RegisterUseCase>((ref) {
  return RegisterUseCase(ref.watch(authRepositoryProvider));
});

final logoutUseCaseProvider = Provider<LogoutUseCase>((ref) {
  return LogoutUseCase(ref.watch(authRepositoryProvider));
});

final forgotPasswordUseCaseProvider = Provider<ForgotPasswordUseCase>((ref) {
  return ForgotPasswordUseCase(ref.watch(authRepositoryProvider));
});

final resetPasswordUseCaseProvider = Provider<ResetPasswordUseCase>((ref) {
  return ResetPasswordUseCase(ref.watch(authRepositoryProvider));
});

final checkAuthStatusUseCaseProvider = Provider<CheckAuthStatusUseCase>((ref) {
  return CheckAuthStatusUseCase(ref.watch(authRepositoryProvider));
});

final authControllerProvider = Provider<AuthController>((ref) {
  return AuthController(ref.watch(authRepositoryProvider));
});

/// Prefer [authNotifierProvider] for UI that needs live session state.
final currentUserProvider = FutureProvider<AuthUser?>((ref) async {
  final authAsync = ref.watch(authNotifierProvider);
  return authAsync.when(
    data: (state) => state.user,
    loading: () => null,
    error: (_, __) => null,
  );
});
