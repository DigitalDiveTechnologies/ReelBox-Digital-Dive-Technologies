import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';

/// Reads Android ACTION_SEND (text/plain) payloads via platform channel.
///
/// Channel: `com.example.mobile/share_intent`
/// - `getInitialSharedText` → String? (cold start / first open)
/// - native → Flutter `onSharedText` (warm share while app is alive)
class AndroidShareIntentDataSource {
  AndroidShareIntentDataSource({
    MethodChannel? channel,
  }) : _channel = channel ?? const MethodChannel(_channelName) {
    _channel.setMethodCallHandler(_onMethodCall);
  }

  static const String _channelName = 'com.example.mobile/share_intent';
  static const String _methodGetInitial = 'getInitialSharedText';
  static const String _methodOnShared = 'onSharedText';

  final MethodChannel _channel;
  final StreamController<String> _sharedTextController =
      StreamController<String>.broadcast();

  /// Warm share delivered before a listener attaches (engine vs widget race).
  String? _bufferedWarmText;
  bool _hasStreamConsumer = false;

  /// Subsequent share intents while the process is already running.
  Stream<String> watchSharedText() async* {
    _hasStreamConsumer = true;
    final buffered = _bufferedWarmText;
    if (buffered != null) {
      _bufferedWarmText = null;
      yield buffered;
    }
    yield* _sharedTextController.stream;
  }

  /// One-shot read of the shared text that launched (or resumed) the activity.
  Future<String?> getInitialSharedText() async {
    if (kIsWeb) return null;
    if (defaultTargetPlatform != TargetPlatform.android) return null;

    try {
      final text = await _channel.invokeMethod<String>(_methodGetInitial);
      final trimmed = text?.trim();
      if (trimmed == null || trimmed.isEmpty) return null;
      return trimmed;
    } on MissingPluginException {
      return null;
    } on PlatformException {
      return null;
    }
  }

  Future<dynamic> _onMethodCall(MethodCall call) async {
    if (call.method != _methodOnShared) return null;

    final raw = call.arguments;
    if (raw is! String) return null;

    final trimmed = raw.trim();
    if (trimmed.isEmpty) return null;

    if (_hasStreamConsumer && !_sharedTextController.isClosed) {
      _sharedTextController.add(trimmed);
    } else {
      _bufferedWarmText = trimmed;
    }
    return null;
  }

  /// Releases the method-call handler and stream resources.
  void dispose() {
    _channel.setMethodCallHandler(null);
    _sharedTextController.close();
  }
}
