import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';

import '../../../../core/network/media_url_resolver.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_platform.dart';
import '../../../../shared/widgets/instagram_icon.dart';

/// Related-reels card for Media Detail only.
///
/// Matches Library card size/look, but:
/// - Platform pill overlays top-left (like Media Detail preview)
/// - No Completed/status badge
/// - Does not modify [LibraryMediaCard]
class RelatedReelCard extends StatelessWidget {
  const RelatedReelCard({
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

  String? get _resolvedThumbnailUrl {
    final raw = item.thumbnailUrl?.trim();
    if (raw == null || raw.isEmpty) return null;
    try {
      return resolveSignedMediaUrl(raw).toString();
    } catch (_) {
      return null;
    }
  }

  @override
  Widget build(BuildContext context) {
    final thumbUrl = _resolvedThumbnailUrl;
    final isIg = item.platform == MediaPlatform.instagram;

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
              if (thumbUrl != null)
                Positioned.fill(
                  child: ClipRRect(
                    borderRadius: AppRadius.circularCard,
                    child: CachedNetworkImage(
                      cacheKey: 'thumb-${item.id}',
                      imageUrl: thumbUrl,
                      fit: BoxFit.cover,
                      memCacheWidth: 480,
                      memCacheHeight: 680,
                      fadeInDuration: Duration.zero,
                      fadeOutDuration: Duration.zero,
                      placeholder: (context, url) => const SizedBox.shrink(),
                      errorWidget: (context, url, error) =>
                          const SizedBox.shrink(),
                    ),
                  ),
                )
              else if (item.hasThumbnailKey)
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
                    vertical: AppSpacing.xs,
                  ),
                  decoration: BoxDecoration(
                    color: AppColors.splashBgDeep.withValues(alpha: 0.45),
                    borderRadius: AppRadius.circularPill,
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      isIg
                          ? const InstagramIcon(size: 14)
                          : const Icon(
                              Icons.facebook,
                              size: 14,
                              color: AppColors.splashTextPrimary,
                            ),
                      const SizedBox(width: AppSpacing.xs),
                      Text(
                        item.platform.label,
                        style: const TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                          color: AppColors.splashTextPrimary,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              Center(
                child: Container(
                  width: 44,
                  height: 44,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color:
                        AppColors.splashTextPrimary.withValues(alpha: 0.2),
                    border: Border.all(
                      color: AppColors.splashTextPrimary.withValues(alpha: 0.5),
                    ),
                  ),
                  child: const Icon(
                    Icons.play_arrow_rounded,
                    color: AppColors.splashTextPrimary,
                    size: 28,
                  ),
                ),
              ),
              Positioned(
                left: AppSpacing.md,
                bottom: AppSpacing.md,
                right: AppSpacing.md,
                child: Text(
                  item.createdDateLabel,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w500,
                    color: AppColors.splashTextMuted.withValues(alpha: 0.95),
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
