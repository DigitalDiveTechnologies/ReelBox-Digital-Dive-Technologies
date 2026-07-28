import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/widgets/app_back_button.dart';
import '../../../../shared/widgets/app_empty_state.dart';

/// In-app notifications list (local / future-ready; no push yet).
///
/// Background matches Home / Library dark glassmorphic gradient scheme.
class NotificationsPage extends StatelessWidget {
  const NotificationsPage({super.key});

  /// Placeholder for future local/push notification items.
  static const List<NotificationItem> items = <NotificationItem>[];

  @override
  Widget build(BuildContext context) {
    final horizontal = AppBackButton.horizontalInset(context);

    return Scaffold(
      backgroundColor: AppColors.splashBgDeep,
      body: Stack(
        fit: StackFit.expand,
        children: [
          const DecoratedBox(
            decoration: BoxDecoration(gradient: AppGradients.splashBackground),
          ),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: const Alignment(-0.85, -0.95),
                radius: 0.95,
                colors: [
                  AppColors.splashBgMahogany.withValues(alpha: 0.5),
                  AppColors.splashBgMahogany.withValues(alpha: 0),
                ],
              ),
            ),
          ),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: const Alignment(0.9, -0.7),
                radius: 0.9,
                colors: [
                  AppColors.brandPurple.withValues(alpha: 0.18),
                  AppColors.brandPurple.withValues(alpha: 0),
                ],
              ),
            ),
          ),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: const Alignment(0.6, 0.95),
                radius: 0.85,
                colors: [
                  AppColors.splashBgNavy.withValues(alpha: 0.55),
                  AppColors.splashBgNavy.withValues(alpha: 0),
                ],
              ),
            ),
          ),
          SafeArea(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const AppBackButtonHeader(),
                Padding(
                  padding: EdgeInsets.fromLTRB(
                    horizontal,
                    AppBackButton.gapBelow,
                    horizontal,
                    AppSpacing.sm,
                  ),
                  child: const Text(
                    'Notifications',
                    style: TextStyle(
                      fontSize: 28,
                      fontWeight: FontWeight.w700,
                      letterSpacing: -0.4,
                      color: AppColors.splashTextPrimary,
                    ),
                  ),
                ),
                Expanded(
                  child: items.isEmpty
                      ? const Center(
                          child: AppEmptyState(
                            icon: Icons.notifications_none_rounded,
                            title: 'No notifications yet',
                            message:
                                'Download updates and account alerts will show up here.',
                          ),
                        )
                      : ListView.separated(
                          padding: EdgeInsets.fromLTRB(
                            horizontal,
                            AppSpacing.sm,
                            horizontal,
                            AppSpacing.xxl,
                          ),
                          itemCount: items.length,
                          separatorBuilder: (_, _) =>
                              const SizedBox(height: AppSpacing.sm),
                          itemBuilder: (context, index) {
                            final item = items[index];
                            return _NotificationTile(item: item);
                          },
                        ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Future-ready notification model (local store / push payload later).
class NotificationItem {
  const NotificationItem({
    required this.id,
    required this.title,
    required this.body,
    required this.createdAt,
    this.isRead = false,
  });

  final String id;
  final String title;
  final String body;
  final DateTime createdAt;
  final bool isRead;
}

class _NotificationTile extends StatelessWidget {
  const _NotificationTile({required this.item});

  final NotificationItem item;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: AppRadius.circularCard,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: AppColors.splashSheet.withValues(alpha: 0.88),
          borderRadius: AppRadius.circularCard,
          border: Border.all(
            color: AppColors.splashChipBorder.withValues(alpha: 0.65),
          ),
        ),
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.md),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                item.title,
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  color: AppColors.splashTextPrimary,
                ),
              ),
              const SizedBox(height: AppSpacing.xs),
              Text(
                item.body,
                style: TextStyle(
                  fontSize: 13,
                  color: AppColors.splashTextMuted.withValues(alpha: 0.95),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
