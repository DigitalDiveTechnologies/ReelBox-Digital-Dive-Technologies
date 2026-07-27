import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_shadows.dart';
import '../../../../core/theme/app_spacing.dart';

/// Gradient play mark with concentric pulse rings (Splash mockup).
class SplashLogo extends StatelessWidget {
  const SplashLogo({
    super.key,
    required this.pulse,
  });

  final Animation<double> pulse;

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: pulse,
      builder: (context, child) {
        final t = pulse.value;
        final outerScale = 1.0 + (t * 0.04);
        final outerOpacity = 0.22 + (t * 0.18);
        final midScale = 1.0 + (t * 0.025);
        final midOpacity = 0.35 + (t * 0.2);

        return SizedBox(
          width: AppSpacing.splashRingOuter,
          height: AppSpacing.splashRingOuter,
          child: Stack(
            alignment: Alignment.center,
            children: [
              Transform.scale(
                scale: outerScale,
                child: Container(
                  width: AppSpacing.splashRingOuter,
                  height: AppSpacing.splashRingOuter,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: Border.all(
                      color: AppColors.splashTextPrimary.withValues(
                        alpha: outerOpacity,
                      ),
                      width: 1,
                    ),
                  ),
                ),
              ),
              Transform.scale(
                scale: midScale,
                child: Container(
                  width: AppSpacing.splashRingInner,
                  height: AppSpacing.splashRingInner,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: Border.all(
                      color: AppColors.splashTextPrimary.withValues(
                        alpha: midOpacity,
                      ),
                      width: 1.15,
                    ),
                  ),
                ),
              ),
              child!,
            ],
          ),
        );
      },
      child: Container(
        width: AppSpacing.splashLogoSize,
        height: AppSpacing.splashLogoSize,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          gradient: AppGradients.brandMark,
          boxShadow: AppShadows.brandMark(Brightness.dark),
        ),
        child: const Center(
          child: Padding(
            // Optical centering for the play triangle.
            padding: EdgeInsets.only(left: 3),
            child: Icon(
              Icons.play_arrow_rounded,
              size: AppSpacing.splashPlayIcon,
              color: AppColors.splashTextPrimary,
            ),
          ),
        ),
      ),
    );
  }
}
