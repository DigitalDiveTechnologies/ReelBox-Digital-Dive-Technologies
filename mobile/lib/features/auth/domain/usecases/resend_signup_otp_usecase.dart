import '../repositories/auth_repository.dart';

class ResendSignupOtpUseCase {
  const ResendSignupOtpUseCase(this._repository);

  final AuthRepository _repository;

  Future<String> call({required String email}) {
    return _repository.resendSignupOtp(email: email);
  }
}
