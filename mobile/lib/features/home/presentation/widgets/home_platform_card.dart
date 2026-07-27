import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/models/media_platform.dart';
import '../../../../shared/widgets/instagram_icon.dart';

/// Platform shortcut card (Instagram / Facebook) — Home mockup.
class HomePlatformCard extends StatelessWidget {
  const HomePlatformCard({
    super.key,
    required this.platform,
    required this.savedLabel,
    this.onTap,
  });

  final MediaPlatform platform;
  final String savedLabel;
  final VoidCallback? onTap;

  bool get _isInstagram => platform == MediaPlatform.instagram;

  @override
  Widget build(BuildContext context) {
    final tint = _isInstagram
        ? AppColors.splashBgMahogany
        : AppColors.splashBgNavy;
    final accent = _isInstagram
        ? AppColors.brandPurple
        : AppColors.statusQueued;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: AppRadius.circularCard,
        child: Ink(
          padding: const EdgeInsets.all(AppSpacing.cardPadding),
          decoration: BoxDecoration(
            borderRadius: AppRadius.circularCard,
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                tint.withValues(alpha: 0.85),
                AppColors.splashSheet.withValues(alpha: 0.95),
              ],
            ),
            border: Border.all(color: accent.withValues(alpha: 0.35)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  gradient: _isInstagram
                      ? AppGradients.brandCta
                      : LinearGradient(
                          colors: [
                            AppColors.statusQueued,
                            AppColors.splashBgNavy,
                          ],
                        ),
                  borderRadius: AppRadius.circularMd,
                ),
                child: Center(
                  child: _isInstagram
                      ? const InstagramIcon(size: 18)
                      : const Icon(
                          Icons.facebook,
                          size: 18,
                          color: AppColors.splashTextPrimary,
                        ),
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              Text(
                platform.label,
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  color: AppColors.splashTextPrimary,
                ),
              ),
              const SizedBox(height: AppSpacing.xxs),
              Text(
                savedLabel,
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w400,
                  color: AppColors.splashTextMuted,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
