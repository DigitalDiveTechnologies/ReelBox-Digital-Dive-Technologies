import 'package:flutter/material.dart';

import '../../shared/models/media_status.dart';

/// Shared color tokens. Splash brand palette is design-locked to the mockup.
abstract final class AppColors {
  // ── Existing Material surfaces (unchanged for other screens) ─────────────
  static const Color seed = Color(0xFF0F766E);

  static const Color surfaceLight = Color(0xFFF4F7F6);
  static const Color surfaceDark = Color(0xFF0F1413);
  static const Color surfaceContainerLight = Color(0xFFFFFFFF);
  static const Color surfaceContainerDark = Color(0xFF1A2220);

  static const Color onSurfaceLight = Color(0xFF14201E);
  static const Color onSurfaceDark = Color(0xFFE4EBE9);

  static const Color outlineLight = Color(0xFFC5D0CD);
  static const Color outlineDark = Color(0xFF3A4744);

  static const Color surfaceContainerLowLight = Color(0xFFEEF3F1);
  static const Color surfaceContainerLightMid = Color(0xFFE6EEEC);
  static const Color surfaceContainerLowDark = Color(0xFF151C1A);
  static const Color surfaceContainerDarkMid = Color(0xFF1E2725);

  // ── Splash / brand (design-locked mockup) ────────────────────────────────
  static const Color splashBgDeep = Color(0xFF12121B);
  static const Color splashBgMahogany = Color(0xFF2C1A2E);
  static const Color splashBgNavy = Color(0xFF0F1A2E);
  static const Color splashSheet = Color(0xFF161A25);
  static const Color splashTextPrimary = Color(0xFFFFFFFF);
  static const Color splashTextMuted = Color(0xFF9E9E9E);
  static const Color splashChipBorder = Color(0xFF333333);
  static const Color splashChipFill = Color(0xFF1A1A22);
  static const Color splashHandle = Color(0xFF5A5A68);
  static const Color splashRing = Color(0xFF4A4A58);
  static const Color brandOrange = Color(0xFFFF7D3C);
  static const Color brandOrangeDeep = Color(0xFFFF5C33);
  static const Color brandPurple = Color(0xFF9C42FF);
  static const Color brandPurpleDeep = Color(0xFF8E2DE2);

  /// Glass control surfaces (shared back button, etc.).
  /// Derived from splash chip tokens at translucent alpha.
  static const Color glassFill = Color(0xA61A1A22);
  static const Color glassBorder = Color(0xCC333333);

  // ── Status (SRS §6.3 / §13) ──────────────────────────────────────────────
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
