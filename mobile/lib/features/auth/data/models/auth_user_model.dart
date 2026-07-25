/// Data-layer DTO for an authenticated user.
///
/// TODO: Map JSON from backend auth API responses when contracts are available.
class AuthUserModel {
  const AuthUserModel({
    required this.id,
    this.email,
  });

  final String id;
  final String? email;

  /// TODO: Align field names with the backend auth API contract.
  factory AuthUserModel.fromJson(Map<String, dynamic> json) {
    return AuthUserModel(
      id: json['id']?.toString() ?? '',
      email: json['email']?.toString(),
    );
  }

  /// TODO: Align field names with the backend auth API contract.
  Map<String, dynamic> toJson() {
    return <String, dynamic>{
      'id': id,
      if (email != null) 'email': email,
    };
  }
}
