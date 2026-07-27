import 'dart:ui';

import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/theme/app_typography.dart';
import '../../../../shared/widgets/instagram_icon.dart';

enum SplashPlatformKind { instagram, facebook }

/// Glass platform chip matching the Splash mockup.
class SplashPlatformChip extends StatelessWidget {
  const SplashPlatformChip({
    super.key,
    required this.kind,
    required this.label,
  });

  final SplashPlatformKind kind;
  final String label;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: AppRadius.circularPill,
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 12, sigmaY: 12),
        child: Container(
          height: AppSpacing.splashChipHeight,
          padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
          decoration: BoxDecoration(
            color: AppColors.splashChipFill.withValues(alpha: 0.45),
            borderRadius: AppRadius.circularPill,
            border: Border.all(
              color: AppColors.splashChipBorder.withValues(alpha: 0.9),
              width: 1,
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (kind == SplashPlatformKind.instagram)
                const InstagramIcon(size: 16)
              else
                const SizedBox(
                  width: 16,
                  height: 16,
                  child: CustomPaint(
                    painter: _FacebookGlyphPainter(),
                  ),
                ),
              const SizedBox(width: AppSpacing.xs),
              Text(label, style: AppTypography.splashChipLabel),
            ],
          ),
        ),
      ),
    );
  }
}

class _FacebookGlyphPainter extends CustomPainter {
  const _FacebookGlyphPainter();

  @override
  void paint(Canvas canvas, Size size) {
    final stroke = Paint()
      ..color = AppColors.splashTextPrimary
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.35;

    canvas.drawCircle(
      Offset(size.width / 2, size.height / 2),
      size.width * 0.42,
      stroke,
    );

    final textPainter = TextPainter(
      text: const TextSpan(
        text: 'f',
        style: TextStyle(
          color: AppColors.splashTextPrimary,
          fontSize: 11,
          fontWeight: FontWeight.w700,
          height: 1,
        ),
      ),
      textDirection: TextDirection.ltr,
    )..layout();

    textPainter.paint(
      canvas,
      Offset(
        (size.width - textPainter.width) / 2 + 0.5,
        (size.height - textPainter.height) / 2 - 0.5,
      ),
    );
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
