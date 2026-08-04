import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import '../models/auth_session_model.dart';
import '../models/auth_user_model.dart';

/// Remote auth data source (SRS §22 JWT endpoints).
abstract class AuthRemoteDataSource {
  Future<AuthSessionModel> login({
    required String email,
    required String password,
  });

  /// Starts signup and emails OTP. Does not return tokens.
  Future<String> register({
    required String email,
    required String password,
  });

  Future<void> logout();

  Future<AuthUserModel?> getCurrentUser();

  Future<AuthSessionModel> refreshToken({required String refreshToken});

  Future<String> forgotPassword({required String email});

  Future<String> resetPassword({
    required String email,
    required String otp,
    required String newPassword,
  });

  Future<AuthSessionModel> verifySignupOtp({
    required String email,
    required String otp,
  });

  Future<String> resendSignupOtp({required String email});
}

class AuthRemoteDataSourceImpl implements AuthRemoteDataSource {
  AuthRemoteDataSourceImpl(this._api);

  final ApiClient _api;

  @override
  Future<AuthSessionModel> login({
    required String email,
    required String password,
  }) async {
    final json = await _api.postJson(
      ApiEndpoints.authLogin,
      body: {'email': email, 'password': password},
      authenticated: false,
    );
    return AuthSessionModel.fromJson(json);
  }

  @override
  Future<String> register({
    required String email,
    required String password,
  }) async {
    final json = await _api.postJson(
      ApiEndpoints.authRegister,
      body: {'email': email, 'password': password},
      authenticated: false,
    );
    return json['message']?.toString() ??
        'We sent a 6-digit verification code to your email. Enter it to finish signup.';
  }

  @override
  Future<void> logout() async {
    await _api.postJson(ApiEndpoints.authLogout, body: <String, dynamic>{});
  }

  @override
  Future<AuthUserModel?> getCurrentUser() async {
    final json = await _api.getJson(ApiEndpoints.authMe);
    return AuthUserModel.fromJson(json);
  }

  @override
  Future<AuthSessionModel> refreshToken({required String refreshToken}) async {
    final json = await _api.postJson(
      ApiEndpoints.authRefresh,
      body: {'refreshToken': refreshToken},
      authenticated: false,
    );
    return AuthSessionModel.fromJson(json);
  }

  @override
  Future<String> forgotPassword({required String email}) async {
    final json = await _api.postJson(
      ApiEndpoints.authForgotPassword,
      body: {'email': email},
      authenticated: false,
    );
    return json['message']?.toString() ??
        'If an account exists for that email, a reset code has been sent.';
  }

  @override
  Future<String> resetPassword({
    required String email,
    required String otp,
    required String newPassword,
  }) async {
    final json = await _api.postJson(
      ApiEndpoints.authResetPassword,
      body: {
        'email': email,
        'otp': otp,
        'newPassword': newPassword,
      },
      authenticated: false,
    );
    return json['message']?.toString() ??
        'Password updated. You can sign in with your new password.';
  }

  @override
  Future<AuthSessionModel> verifySignupOtp({
    required String email,
    required String otp,
  }) async {
    final json = await _api.postJson(
      ApiEndpoints.authVerifyEmail,
      body: {'email': email, 'otp': otp},
      authenticated: false,
    );
    return AuthSessionModel.fromJson(json);
  }

  @override
  Future<String> resendSignupOtp({required String email}) async {
    final json = await _api.postJson(
      ApiEndpoints.authResendSignupOtp,
      body: {'email': email},
      authenticated: false,
    );
    return json['message']?.toString() ??
        'If an unverified account exists for that email, a new code has been sent.';
  }
}
