import '../entities/auth_user.dart';
import '../repositories/auth_repository.dart';

class VerifySignupOtpUseCase {
  const VerifySignupOtpUseCase(this._repository);

  final AuthRepository _repository;

  Future<AuthUser> call({
    required String email,
    required String otp,
  }) {
    return _repository.verifySignupOtp(email: email, otp: otp);
  }
}
