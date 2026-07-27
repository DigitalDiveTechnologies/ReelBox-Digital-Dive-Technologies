import 'package:flutter/material.dart';

import 'app_colors.dart';

/// Shared gradient tokens. Splash gradients are design-locked to the mockup.
abstract final class AppGradients {
  /// Splash full-screen background (mahogany top → deep navy bottom-right).
  static const LinearGradient splashBackground = LinearGradient(
    begin: Alignment.topCenter,
    end: Alignment.bottomRight,
    colors: [
      AppColors.splashBgMahogany,
      AppColors.splashBgDeep,
      AppColors.splashBgNavy,
    ],
    stops: [0.0, 0.45, 1.0],
  );

  /// Brand mark disc (orange → purple, diagonal).
  static const LinearGradient brandMark = LinearGradient(
    begin: Alignment.topLeft,
    end: Alignment.bottomRight,
    colors: [
      AppColors.brandOrangeDeep,
      AppColors.brandPurpleDeep,
    ],
  );

  /// Primary CTA (orange → purple, horizontal).
  static const LinearGradient brandCta = LinearGradient(
    begin: Alignment.centerLeft,
    end: Alignment.centerRight,
    colors: [
      AppColors.brandOrange,
      AppColors.brandPurple,
    ],
  );
}
