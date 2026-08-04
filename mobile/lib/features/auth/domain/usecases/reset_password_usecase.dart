import '../repositories/auth_repository.dart';

class ResetPasswordUseCase {
  const ResetPasswordUseCase(this._repository);

  final AuthRepository _repository;

  Future<String> call({
    required String email,
    required String otp,
    required String newPassword,
  }) {
    return _repository.resetPassword(
      email: email,
      otp: otp,
      newPassword: newPassword,
    );
  }
}
