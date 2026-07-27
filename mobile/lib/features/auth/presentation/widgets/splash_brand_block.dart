import 'package:flutter/material.dart';

import '../../../../core/constants/app_constants.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../core/theme/app_typography.dart';
import 'splash_logo.dart';
import 'splash_platform_chip.dart';

/// Center brand column for the Splash mockup.
class SplashBrandBlock extends StatelessWidget {
  const SplashBrandBlock({
    super.key,
    required this.pulse,
  });

  final Animation<double> pulse;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.splashHorizontal),
      child: Column(
        children: [
          const Spacer(flex: 3),
          SplashLogo(pulse: pulse),
          const SizedBox(height: AppSpacing.splashLogoToTitle),
          const Text(
            AppConstants.appName,
            textAlign: TextAlign.center,
            style: AppTypography.splashTitle,
          ),
          const SizedBox(height: AppSpacing.splashTitleToTagline),
          const Text(
            'Share. Save. Watch anytime.',
            textAlign: TextAlign.center,
            style: AppTypography.splashTagline,
          ),
          const SizedBox(height: AppSpacing.splashTaglineToChips),
          const Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              SplashPlatformChip(
                kind: SplashPlatformKind.instagram,
                label: 'Instagram',
              ),
              SizedBox(width: AppSpacing.splashChipGap),
              SplashPlatformChip(
                kind: SplashPlatformKind.facebook,
                label: 'Facebook',
              ),
            ],
          ),
          const Spacer(flex: 2),
        ],
      ),
    );
  }
}
