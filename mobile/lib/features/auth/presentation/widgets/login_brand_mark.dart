import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_shadows.dart';
import '../../../../core/theme/app_spacing.dart';

/// Rounded-square play brand mark for the Login mockup.
class LoginBrandMark extends StatelessWidget {
  const LoginBrandMark({super.key});

  static const double _size = 72;
  static const double _iconSize = 34;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: _size,
      height: _size,
      decoration: BoxDecoration(
        gradient: AppGradients.brandCta,
        borderRadius: AppRadius.circularXxxl,
        boxShadow: AppShadows.brandMark(Brightness.dark),
      ),
      child: const Center(
        child: Padding(
          padding: EdgeInsets.only(left: 2),
          child: Icon(
            Icons.play_arrow_rounded,
            size: _iconSize,
            color: AppColors.splashTextPrimary,
          ),
        ),
      ),
    );
  }
}

/// Full-width gradient CTA matching the Login mockup.
class LoginGradientButton extends StatelessWidget {
  const LoginGradientButton({
    super.key,
    required this.label,
    required this.onPressed,
  });

  final String label;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      height: AppSpacing.buttonHeight,
      child: DecoratedBox(
        decoration: BoxDecoration(
          gradient: AppGradients.brandCta,
          borderRadius: AppRadius.circularButton,
          boxShadow: AppShadows.cta,
        ),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: onPressed,
            borderRadius: AppRadius.circularButton,
            child: Center(
              child: Text(
                label,
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.2,
                  color: AppColors.splashTextPrimary,
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
