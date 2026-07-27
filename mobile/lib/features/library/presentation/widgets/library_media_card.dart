import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_platform.dart';
import '../../../../shared/models/media_status.dart';
import '../../../../shared/models/media_status_ui.dart';

/// Library grid card — real API fields (status, platform, created date).
class LibraryMediaCard extends StatelessWidget {
  const LibraryMediaCard({
    super.key,
    required this.item,
    this.onTap,
  });

  final MediaItemPreview item;
  final VoidCallback? onTap;

  LinearGradient get _coverGradient {
    return switch (item.platform) {
      MediaPlatform.instagram => LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [
            AppColors.brandOrangeDeep.withValues(alpha: 0.55),
            AppColors.splashBgMahogany.withValues(alpha: 0.95),
            AppColors.splashBgDeep,
          ],
        ),
      MediaPlatform.facebook => LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [
            AppColors.statusQueued.withValues(alpha: 0.55),
            AppColors.splashBgNavy.withValues(alpha: 0.95),
            AppColors.splashBgDeep,
          ],
        ),
    };
  }

  Color get _statusBadgeColor => AppColors.statusColor(item.status);

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: AppRadius.circularCard,
        child: Ink(
          decoration: BoxDecoration(
            borderRadius: AppRadius.circularCard,
            gradient: _coverGradient,
            border: Border.all(
              color: AppColors.splashChipBorder.withValues(alpha: 0.45),
            ),
          ),
          child: Stack(
            children: [
              // Real thumbnail when signed URL is available (SRS FR-010).
              if (item.hasThumbnailUrl)
                Positioned.fill(
                  child: ClipRRect(
                    borderRadius: AppRadius.circularCard,
                    child: Image.network(
                      item.thumbnailUrl!,
                      fit: BoxFit.cover,
                      errorBuilder: (context, error, stackTrace) =>
                          const SizedBox.shrink(),
                    ),
                  ),
                )
              else if (item.status == MediaStatus.completed &&
                  item.hasThumbnailKey)
                Positioned.fill(
                  child: DecoratedBox(
                    decoration: BoxDecoration(
                      borderRadius: AppRadius.circularCard,
                      color: AppColors.splashBgDeep.withValues(alpha: 0.35),
                    ),
                    child: const Center(
                      child: Icon(
                        Icons.image_outlined,
                        color: AppColors.splashTextPrimary,
                        size: 28,
                      ),
                    ),
                  ),
                ),
              Positioned(
                top: AppSpacing.sm,
                left: AppSpacing.sm,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppSpacing.sm,
                    vertical: AppSpacing.xxs,
                  ),
                  decoration: BoxDecoration(
                    color: AppColors.splashBgDeep.withValues(alpha: 0.55),
                    borderRadius: AppRadius.circularPill,
                  ),
                  child: Text(
                    item.status.label,
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: _statusBadgeColor,
                    ),
                  ),
                ),
              ),
              Center(
                child: Container(
                  width: 44,
                  height: 44,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: AppColors.splashTextPrimary.withValues(alpha: 0.2),
                    border: Border.all(
                      color: AppColors.splashTextPrimary.withValues(alpha: 0.5),
                    ),
                  ),
                  child: Icon(
                    item.status == MediaStatus.failed
                        ? Icons.error_outline_rounded
                        : item.isActive
                            ? item.status.icon
                            : Icons.play_arrow_rounded,
                    color: AppColors.splashTextPrimary,
                    size: 28,
                  ),
                ),
              ),
              Positioned(
                left: AppSpacing.md,
                bottom: AppSpacing.md,
                right: AppSpacing.md,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      item.platform.label,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.splashTextPrimary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      item.createdDateLabel,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.w500,
                        color: AppColors.splashTextMuted.withValues(alpha: 0.95),
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
