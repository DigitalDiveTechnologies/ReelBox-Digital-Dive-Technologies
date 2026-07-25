import 'package:flutter/material.dart';

import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_status.dart';
import '../../../../shared/models/media_status_ui.dart';
import '../../../../shared/widgets/media_thumbnail_placeholder.dart';
import '../../../../shared/widgets/platform_badge.dart';

/// Library media card with thumbnail, badge, date, and status (SRS §7).
class LibraryMediaCard extends StatelessWidget {
  const LibraryMediaCard({
    super.key,
    required this.item,
    required this.dense,
    this.onTap,
  });

  final MediaItemPreview item;
  final bool dense;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    if (dense) {
      return _ListCard(item: item, onTap: onTap);
    }
    return _GridCard(item: item, onTap: onTap);
  }
}

class _GridCard extends StatelessWidget {
  const _GridCard({required this.item, this.onTap});

  final MediaItemPreview item;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(10),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: MediaThumbnailPlaceholder(
                  aspectRatio: 9 / 14,
                  borderRadius: 12,
                  showPlayHint: item.status == MediaStatus.completed,
                  compact: true,
                ),
              ),
              const SizedBox(height: 10),
              Text(
                item.displayTitle,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(context).textTheme.titleSmall,
              ),
              const SizedBox(height: 6),
              Row(
                children: [
                  PlatformBadge(platform: item.platform, compact: true),
                  const Spacer(),
                  Text(
                    item.status.label,
                    style: Theme.of(context).textTheme.labelSmall?.copyWith(
                          color: item.status.color(context),
                          fontWeight: FontWeight.w700,
                        ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Text(
                _formatDate(item.savedAt),
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: scheme.onSurface.withValues(alpha: 0.55),
                    ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ListCard extends StatelessWidget {
  const _ListCard({required this.item, this.onTap});

  final MediaItemPreview item;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              SizedBox(
                width: 72,
                child: MediaThumbnailPlaceholder(
                  aspectRatio: 3 / 4,
                  borderRadius: 12,
                  compact: true,
                  showPlayHint: true,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.displayTitle,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: Theme.of(context).textTheme.titleSmall,
                    ),
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        PlatformBadge(platform: item.platform, compact: true),
                        const SizedBox(width: 8),
                        Text(
                          item.status.label,
                          style: Theme.of(context).textTheme.labelSmall?.copyWith(
                                color: item.status.color(context),
                                fontWeight: FontWeight.w700,
                              ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Text(
                      _formatDate(item.savedAt),
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: scheme.onSurface.withValues(alpha: 0.55),
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

String _formatDate(DateTime value) {
  final local = value.toLocal();
  final y = local.year.toString().padLeft(4, '0');
  final m = local.month.toString().padLeft(2, '0');
  final d = local.day.toString().padLeft(2, '0');
  return '$y-$m-$d';
}
