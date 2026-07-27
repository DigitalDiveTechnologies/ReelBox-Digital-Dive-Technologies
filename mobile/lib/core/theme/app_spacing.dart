import 'package:flutter/widgets.dart';

/// Shared spacing scale (logical pixels).
abstract final class AppSpacing {
  static const double xxs = 4;
  static const double xs = 8;
  static const double sm = 12;
  static const double md = 16;
  static const double lg = 20;
  static const double xl = 24;
  static const double xxl = 28;
  static const double xxxl = 32;
  static const double huge = 40;
  static const double massive = 48;

  /// Screen horizontal inset (compact phones → 18, default → 20, wide → 32).
  static const double screenHorizontalCompact = 18;
  static const double screenHorizontal = 20;

  /// Vertical gap between major sections.
  static const double section = 28;

  /// Standard internal card padding.
  static const double cardPadding = 16;

  /// Primary action button height.
  static const double buttonHeight = 50;

  static double horizontalInset(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;
    if (width < 360) return screenHorizontalCompact;
    if (width >= 900) return xxxl;
    return screenHorizontal;
  }

  /// Splash-specific layout.
  static const double splashHorizontal = 20;
  static const double splashLogoToTitle = 24;
  static const double splashTitleToTagline = 10;
  static const double splashTaglineToChips = 24;
  static const double splashChipGap = 12;
  static const double splashSheetHorizontal = 20;
  static const double splashSheetTop = 14;
  static const double splashHandleToBody = 16;
  static const double splashBodyToCta = 20;
  static const double splashSheetBottom = 24;
  static const double splashLogoSize = 88;
  static const double splashRingOuter = 128;
  static const double splashRingInner = 108;
  static const double splashPlayIcon = 36;
  static const double splashHandleWidth = 40;
  static const double splashHandleHeight = 4;
  static const double splashCtaHeight = buttonHeight;
  static const double splashChipHeight = 40;
}
