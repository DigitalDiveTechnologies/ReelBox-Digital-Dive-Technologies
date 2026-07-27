import 'package:flutter/material.dart';

import 'app_typography.dart';

/// Backward-compatible alias — prefer [AppTypography].
abstract final class AppTextStyles {
  static TextTheme textTheme(TextTheme base) => AppTypography.textTheme(base);
}
