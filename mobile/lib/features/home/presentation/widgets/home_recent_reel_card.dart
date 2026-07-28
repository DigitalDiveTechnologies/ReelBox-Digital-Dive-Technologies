import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';

import '../../../../core/network/media_url_resolver.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_platform.dart';
import '../../../../shared/widgets/instagram_icon.dart';

/// Horizontal recent reel thumbnail card (Home mockup).
class HomeRecentReelCard extends StatelessWidget {
  const HomeRecentReelCard({
    super.key,
    required this.item,
    required this.onTap,
  });

  final MediaItemPreview item;
  final VoidCallback onTap;

  LinearGradient get _coverGradient {
    return switch (item.platform) {
      MediaPlatform.instagram => LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [
            AppColors.brandOrangeDeep.withValues(alpha: 0.75),
            AppColors.splashBgDeep,
          ],
        ),
      MediaPlatform.facebook => LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          colors: [
            AppColors.statusQueued.withValues(alpha: 0.75),
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
    final isIg = item.platform == MediaPlatform.instagram;
    final thumbUrl = _resolvedThumbnailUrl;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: AppRadius.circularCard,
        child: Ink(
          width: 128,
          height: 176,
          decoration: BoxDecoration(
            borderRadius: AppRadius.circularCard,
            gradient: _coverGradient,
            border: Border.all(
              color: AppColors.splashChipBorder.withValues(alpha: 0.55),
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
                      memCacheWidth: 256,
                      memCacheHeight: 352,
                      fadeInDuration: Duration.zero,
                      fadeOutDuration: Duration.zero,
                      placeholder: (context, url) => const SizedBox.shrink(),
                      errorWidget: (context, url, error) =>
                          const SizedBox.shrink(),
                    ),
                  ),
                ),
              Positioned(
                top: AppSpacing.sm,
                left: AppSpacing.sm,
                child: Container(
                  width: 28,
                  height: 28,
                  decoration: BoxDecoration(
                    color: AppColors.splashBgDeep.withValues(alpha: 0.45),
                    borderRadius: AppRadius.circularSm,
                  ),
                  child: Center(
                    child: isIg
                        ? const InstagramIcon(size: 14)
                        : const Icon(
                            Icons.facebook,
                            size: 14,
                            color: AppColors.splashTextPrimary,
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
                    color: AppColors.splashTextPrimary.withValues(alpha: 0.22),
                    border: Border.all(
                      color: AppColors.splashTextPrimary.withValues(alpha: 0.55),
                    ),
                  ),
                  child: const Icon(
                    Icons.play_arrow_rounded,
                    color: AppColors.splashTextPrimary,
                    size: 28,
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
