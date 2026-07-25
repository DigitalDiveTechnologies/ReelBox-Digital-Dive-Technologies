/// Supported source platforms for MVP (SRS §1 / FR-003).
enum MediaPlatform {
  instagram,
  facebook,
}

extension MediaPlatformUi on MediaPlatform {
  String get label => switch (this) {
        MediaPlatform.instagram => 'Instagram',
        MediaPlatform.facebook => 'Facebook',
      };

  String get shortLabel => switch (this) {
        MediaPlatform.instagram => 'IG',
        MediaPlatform.facebook => 'FB',
      };
}
