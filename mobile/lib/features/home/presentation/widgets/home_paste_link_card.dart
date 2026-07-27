import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';

/// "Paste a link" action card (Home mockup).
class HomePasteLinkCard extends StatelessWidget {
  const HomePasteLinkCard({
    super.key,
    required this.onTap,
  });

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: AppRadius.circularCard,
        child: Ink(
          width: double.infinity,
          padding: const EdgeInsets.all(AppSpacing.cardPadding),
          decoration: BoxDecoration(
            borderRadius: AppRadius.circularCard,
            gradient: LinearGradient(
              begin: Alignment.centerLeft,
              end: Alignment.centerRight,
              colors: [
                AppColors.splashBgMahogany.withValues(alpha: 0.55),
                AppColors.splashSheet.withValues(alpha: 0.9),
                AppColors.splashBgNavy.withValues(alpha: 0.5),
              ],
            ),
            border: Border.all(
              color: AppColors.brandPurple.withValues(alpha: 0.35),
            ),
          ),
          child: Row(
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  gradient: AppGradients.brandCta,
                  borderRadius: AppRadius.circularLg,
                ),
                child: const Icon(
                  Icons.add_rounded,
                  color: AppColors.splashTextPrimary,
                  size: 28,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              const Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Paste a link',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w700,
                        color: AppColors.splashTextPrimary,
                      ),
                    ),
                    SizedBox(height: AppSpacing.xxs),
                    Text(
                      'Or use the share menu',
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w400,
                        color: AppColors.splashTextMuted,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
