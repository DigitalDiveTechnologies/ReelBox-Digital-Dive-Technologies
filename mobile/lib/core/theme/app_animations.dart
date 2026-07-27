import 'package:flutter/material.dart';

/// Shared animation tokens.
abstract final class AppAnimations {
  static const Duration fast = Duration(milliseconds: 180);
  static const Duration normal = Duration(milliseconds: 280);
  static const Duration slow = Duration(milliseconds: 450);

  /// Splash brand entrance (must stay compatible with existing entry timing).
  static const Duration splashEntrance = Duration(milliseconds: 700);
  static const Duration splashSessionPause = Duration(milliseconds: 150);

  static const Curve standard = Curves.easeOutCubic;
  static const Curve emphasized = Curves.easeOut;
}
