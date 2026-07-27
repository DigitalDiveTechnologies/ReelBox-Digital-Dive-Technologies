import 'auth_user_model.dart';

class AuthTokensModel {
  const AuthTokensModel({
    required this.accessToken,
    required this.refreshToken,
  });

  final String accessToken;
  final String refreshToken;

  factory AuthTokensModel.fromJson(Map<String, dynamic> json) {
    return AuthTokensModel(
      accessToken: json['accessToken']?.toString() ?? '',
      refreshToken: json['refreshToken']?.toString() ?? '',
    );
  }
}

class AuthSessionModel {
  const AuthSessionModel({
    required this.user,
    required this.tokens,
  });

  final AuthUserModel user;
  final AuthTokensModel tokens;

  factory AuthSessionModel.fromJson(Map<String, dynamic> json) {
    final userJson = json['user'];
    final tokensJson = json['tokens'];
    return AuthSessionModel(
      user: AuthUserModel.fromJson(
        userJson is Map<String, dynamic> ? userJson : const <String, dynamic>{},
      ),
      tokens: AuthTokensModel.fromJson(
        tokensJson is Map<String, dynamic>
            ? tokensJson
            : const <String, dynamic>{},
      ),
    );
  }
}
