import 'android_share_intent_datasource.dart';

/// Ensures the Android share [MethodChannel] handler exists before [runApp].
///
/// Without this, a warm [ACTION_SEND] can arrive before the widget tree mounts
/// and the inbound `onSharedText` call is dropped.
class AndroidShareIntentRegistrar {
  AndroidShareIntentRegistrar._();

  static final AndroidShareIntentRegistrar instance =
      AndroidShareIntentRegistrar._();

  AndroidShareIntentDataSource? _dataSource;

  AndroidShareIntentDataSource get dataSource {
    final existing = _dataSource;
    if (existing != null) {
      return existing;
    }
    throw StateError(
      'Call AndroidShareIntentRegistrar.instance.ensureInitialized() in main()',
    );
  }

  void ensureInitialized() {
    _dataSource ??= AndroidShareIntentDataSource();
  }
}
