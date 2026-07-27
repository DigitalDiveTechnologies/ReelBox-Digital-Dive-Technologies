/// Data-layer DTO for an authenticated user (backend `UserResponse`).
class AuthUserModel {
  const AuthUserModel({
    required this.id,
    this.email,
  });

  final String id;
  final String? email;

  factory AuthUserModel.fromJson(Map<String, dynamic> json) {
    return AuthUserModel(
      id: json['id']?.toString() ?? '',
      email: json['email']?.toString(),
    );
  }

  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      'id': id,
      if (email != null) 'email': email,
    };
  }
}
