import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/constants/app_constants.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_shadows.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../notifications/presentation/providers/notification_providers.dart';

/// Top bar: brand mark + ReelBox + notification (Home mockup).
class HomeHeaderBar extends ConsumerWidget {
  const HomeHeaderBar({super.key, required this.onNotificationTap});

  final VoidCallback onNotificationTap;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final unreadCount = ref.watch(unreadNotificationCountProvider);
    final hasUnread = unreadCount > 0;

    return Row(
      children: [
        Container(
          width: 36,
          height: 36,
          decoration: BoxDecoration(
            gradient: AppGradients.brandCta,
            borderRadius: AppRadius.circularMd,
            boxShadow: AppShadows.brandMark(Brightness.dark),
          ),
          child: const Icon(
            Icons.play_arrow_rounded,
            size: 22,
            color: AppColors.splashTextPrimary,
          ),
        ),
        const SizedBox(width: AppSpacing.sm),
        const Text(
          AppConstants.appName,
          style: TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.w700,
            letterSpacing: -0.2,
            color: AppColors.splashTextPrimary,
          ),
        ),
        const Spacer(),
        Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: onNotificationTap,
            customBorder: const CircleBorder(),
            child: Ink(
              width: 40,
              height: 40,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: AppColors.splashChipFill.withValues(alpha: 0.65),
                border: Border.all(
                  color: AppColors.splashChipBorder.withValues(alpha: 0.8),
                ),
              ),
              child: Stack(
                alignment: Alignment.center,
                clipBehavior: Clip.none,
                children: [
                  Icon(
                    hasUnread
                        ? Icons.notifications_rounded
                        : Icons.notifications_none_rounded,
                    size: 20,
                    color: AppColors.splashTextPrimary,
                  ),
                  if (hasUnread)
                    Positioned(
                      top: 6,
                      right: 6,
                      child: SizedBox(
                        width: 8,
                        height: 8,
                        child: DecoratedBox(
                          decoration: BoxDecoration(
                            color: AppColors.brandOrange,
                            shape: BoxShape.circle,
                            border: Border.all(
                              color: AppColors.splashBgDeep,
                              width: 1.5,
                            ),
                          ),
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }
}
