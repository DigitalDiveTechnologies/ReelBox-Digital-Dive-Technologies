import 'package:flutter/material.dart';

import '../../shared/models/media_status.dart';

/// Design tokens for Material 3 light/dark themes (SRS §7 UI).
abstract final class AppColors {
  static const Color seed = Color(0xFF0F766E);

  static const Color surfaceLight = Color(0xFFF4F7F6);
  static const Color surfaceDark = Color(0xFF0F1413);
  static const Color surfaceContainerLight = Color(0xFFFFFFFF);
  static const Color surfaceContainerDark = Color(0xFF1A2220);

  static const Color onSurfaceLight = Color(0xFF14201E);
  static const Color onSurfaceDark = Color(0xFFE4EBE9);

  static const Color outlineLight = Color(0xFFC5D0CD);
  static const Color outlineDark = Color(0xFF3A4744);

  /// Status colors for SRS §6.3 / §13 media states.
  static const Color statusPreparing = Color(0xFF64748B);
  static const Color statusQueued = Color(0xFF2563EB);
  static const Color statusDownloading = Color(0xFF0D9488);
  static const Color statusProcessing = Color(0xFF7C3AED);
  static const Color statusCompleted = Color(0xFF15803D);
  static const Color statusFailed = Color(0xFFDC2626);

  static Color statusColor(MediaStatus status) {
    return switch (status) {
      MediaStatus.preparing => statusPreparing,
      MediaStatus.queued => statusQueued,
      MediaStatus.downloading => statusDownloading,
      MediaStatus.processing => statusProcessing,
      MediaStatus.completed => statusCompleted,
      MediaStatus.failed => statusFailed,
    };
  }

  static Color statusContainer(MediaStatus status, Brightness brightness) {
    final base = statusColor(status);
    return brightness == Brightness.light
        ? base.withValues(alpha: 0.12)
        : base.withValues(alpha: 0.22);
  }
}
