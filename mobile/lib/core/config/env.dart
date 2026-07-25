/// Application environment flavors.
enum Env {
  dev,
  staging,
  prod;

  static Env fromString(String value) {
    switch (value.toLowerCase()) {
      case 'staging':
        return Env.staging;
      case 'prod':
      case 'production':
        return Env.prod;
      case 'dev':
      case 'development':
      default:
        return Env.dev;
    }
  }
}
