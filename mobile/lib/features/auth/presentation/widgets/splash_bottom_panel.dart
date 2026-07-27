import 'dart:ui';

import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_shadows.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/theme/app_typography.dart';

/// Bottom glass sheet with handle, copy, and Get started CTA (Splash mockup).
class SplashBottomPanel extends StatelessWidget {
  const SplashBottomPanel({
    super.key,
    required this.isResolving,
    required this.onGetStarted,
  });

  final bool isResolving;
  final VoidCallback onGetStarted;

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.paddingOf(context).bottom;

    return ClipRRect(
      borderRadius: AppRadius.sheetTop,
      child: BackdropFilter(
        filter: ImageFilter.blur(sigmaX: 28, sigmaY: 28),
        child: Container(
          width: double.infinity,
          decoration: BoxDecoration(
            color: AppColors.splashSheet.withValues(alpha: 0.92),
            borderRadius: AppRadius.sheetTop,
            border: Border(
              top: BorderSide(
                color: AppColors.splashChipBorder.withValues(alpha: 0.55),
              ),
            ),
          ),
          padding: EdgeInsets.fromLTRB(
            AppSpacing.splashSheetHorizontal,
            AppSpacing.splashSheetTop,
            AppSpacing.splashSheetHorizontal,
            AppSpacing.splashSheetBottom + bottomInset,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: AppSpacing.splashHandleWidth,
                height: AppSpacing.splashHandleHeight,
                decoration: BoxDecoration(
                  color: AppColors.splashHandle.withValues(alpha: 0.85),
                  borderRadius: AppRadius.circularPill,
                ),
              ),
              const SizedBox(height: AppSpacing.splashHandleToBody),
              Text(
                'Share a reel from Instagram or Facebook — it saves straight to your library.',
                textAlign: TextAlign.center,
                style: AppTypography.splashSheetBody.copyWith(
                  color: AppColors.splashTextPrimary.withValues(alpha: 0.92),
                ),
              ),
              const SizedBox(height: AppSpacing.splashBodyToCta),
              SizedBox(
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
                      onTap: isResolving ? null : onGetStarted,
                      borderRadius: AppRadius.circularButton,
                      child: const Center(
                        child: Text('Get started', style: AppTypography.splashCta),
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
