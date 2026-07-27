import 'package:flutter/material.dart';

/// Shared corner-radius tokens.
abstract final class AppRadius {
  static const double xs = 8;
  static const double sm = 10;
  static const double md = 12;
  static const double lg = 14;
  static const double xl = 16;
  static const double button = 18;
  static const double card = 20;
  static const double xxl = 20;
  static const double xxxl = 24;
  static const double sheet = 24;
  static const double pill = 999;

  static BorderRadius get circularXs => BorderRadius.circular(xs);
  static BorderRadius get circularSm => BorderRadius.circular(sm);
  static BorderRadius get circularMd => BorderRadius.circular(md);
  static BorderRadius get circularLg => BorderRadius.circular(lg);
  static BorderRadius get circularXl => BorderRadius.circular(xl);
  static BorderRadius get circularButton => BorderRadius.circular(button);
  static BorderRadius get circularCard => BorderRadius.circular(card);
  static BorderRadius get circularXxl => BorderRadius.circular(xxl);
  static BorderRadius get circularXxxl => BorderRadius.circular(xxxl);
  static BorderRadius get circularPill => BorderRadius.circular(pill);

  static BorderRadius get sheetTop => const BorderRadius.vertical(
        top: Radius.circular(sheet),
      );
}
