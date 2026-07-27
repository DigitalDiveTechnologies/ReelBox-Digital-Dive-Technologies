import 'package:flutter/material.dart';

import 'app_colors.dart';

/// Shared shadow tokens.
abstract final class AppShadows {
  static List<BoxShadow> brandMark(Brightness brightness) {
    final glow = AppColors.brandPurple.withValues(
      alpha: brightness == Brightness.dark ? 0.35 : 0.28,
    );
    return [
      BoxShadow(
        color: glow,
        blurRadius: 28,
        offset: const Offset(0, 12),
      ),
      BoxShadow(
        color: AppColors.brandOrange.withValues(alpha: 0.18),
        blurRadius: 18,
        offset: const Offset(0, 6),
      ),
    ];
  }

  static List<BoxShadow> get cta => [
        BoxShadow(
          color: AppColors.brandPurple.withValues(alpha: 0.35),
          blurRadius: 20,
          offset: const Offset(0, 10),
        ),
      ];
}
