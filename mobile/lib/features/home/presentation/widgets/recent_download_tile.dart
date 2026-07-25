import 'package:flutter/material.dart';

import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_status_ui.dart';
import '../../../../shared/widgets/media_thumbnail_placeholder.dart';
import '../../../../shared/widgets/platform_badge.dart';

class RecentDownloadTile extends StatelessWidget {
  const RecentDownloadTile({
    super.key,
    required this.item,
    this.onTap,
  });

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
                width: 64,
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
              Icon(
                Icons.chevron_right_rounded,
                color: scheme.onSurface.withValues(alpha: 0.35),
              ),
            ],
          ),
        ),
      ),
    );
  }

  String _formatDate(DateTime value) {
    final local = value.toLocal();
    final y = local.year.toString().padLeft(4, '0');
    final m = local.month.toString().padLeft(2, '0');
    final d = local.day.toString().padLeft(2, '0');
    final hh = local.hour.toString().padLeft(2, '0');
    final mm = local.minute.toString().padLeft(2, '0');
    return '$y-$m-$d · $hh:$mm';
  }
}
