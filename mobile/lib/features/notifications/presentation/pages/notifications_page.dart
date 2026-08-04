import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/widgets/app_back_button.dart';
import '../../../../shared/widgets/app_empty_state.dart';
import '../../domain/models/notification_item.dart';
import '../providers/notification_providers.dart';

/// In-app notifications list (download completion + account alerts).
///
/// Background matches Home / Library dark glassmorphic gradient scheme.
class NotificationsPage extends ConsumerWidget {
  const NotificationsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final horizontal = AppBackButton.horizontalInset(context);
    final asyncItems = ref.watch(notificationsListProvider);

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
                  child: asyncItems.when(
                    loading: () => const Center(
                      child: CircularProgressIndicator(
                        color: AppColors.splashTextPrimary,
                      ),
                    ),
                    error: (error, _) => Center(
                      child: Padding(
                        padding: EdgeInsets.symmetric(horizontal: horizontal),
                        child: _brandThemed(
                          context,
                          child: AppEmptyState(
                            icon: Icons.notifications_off_outlined,
                            title: 'Could not load notifications',
                            message: error.toString(),
                          ),
                        ),
                      ),
                    ),
                    data: (items) {
                      if (items.isEmpty) {
                        return Center(
                          child: _brandThemed(
                            context,
                            child: const AppEmptyState(
                              icon: Icons.notifications_none_rounded,
                              title: 'No notifications yet',
                              message:
                                  'Download updates and account alerts will show up here.',
                            ),
                          ),
                        );
                      }

                      return RefreshIndicator(
                        color: AppColors.splashTextPrimary,
                        backgroundColor: AppColors.splashBgNavy,
                        onRefresh: () async {
                          ref.invalidate(notificationsListProvider);
                          await ref.read(notificationsListProvider.future);
                        },
                        child: ListView.separated(
                          physics: const AlwaysScrollableScrollPhysics(),
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
                            return _NotificationTile(item: items[index]);
                          },
                        ),
                      );
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

  /// Splash/brand colors for empty-state icon (no Material teal seed).
  static Widget _brandThemed(BuildContext context, {required Widget child}) {
    final base = Theme.of(context);
    return Theme(
      data: base.copyWith(
        colorScheme: base.colorScheme.copyWith(
          primary: AppColors.brandPurple,
          primaryContainer: AppColors.brandPurpleDeep,
          onSurface: AppColors.splashTextPrimary,
        ),
        textTheme: base.textTheme.copyWith(
          titleMedium: base.textTheme.titleMedium?.copyWith(
            color: AppColors.splashTextPrimary,
            fontWeight: FontWeight.w700,
          ),
          bodyMedium: base.textTheme.bodyMedium?.copyWith(
            color: AppColors.splashTextMuted.withValues(alpha: 0.95),
          ),
        ),
      ),
      child: child,
    );
  }
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
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Text(
                      item.title,
                      style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w700,
                        color: AppColors.splashTextPrimary,
                      ),
                    ),
                  ),
                  const SizedBox(width: AppSpacing.sm),
                  Text(
                    _formatUtcTimestamp(item.createdAt),
                    style: TextStyle(
                      fontSize: 11,
                      color: AppColors.splashTextMuted.withValues(alpha: 0.85),
                    ),
                  ),
                ],
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

  static String _formatUtcTimestamp(DateTime value) {
    final utc = value.isUtc ? value : value.toUtc();
    final y = utc.year.toString().padLeft(4, '0');
    final m = utc.month.toString().padLeft(2, '0');
    final d = utc.day.toString().padLeft(2, '0');
    final hh = utc.hour.toString().padLeft(2, '0');
    final mm = utc.minute.toString().padLeft(2, '0');
    return '$y-$m-$d $hh:$mm UTC';
  }
}
