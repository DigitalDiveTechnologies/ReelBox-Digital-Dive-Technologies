import 'package:flutter/material.dart';

import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_status.dart';
import '../../../../shared/models/media_status_ui.dart';
import '../../../../shared/widgets/platform_badge.dart';

/// Download status card for all SRS §6.3 / §13 states.
class DownloadStatusCard extends StatelessWidget {
  const DownloadStatusCard({
    super.key,
    required this.item,
    this.onTap,
    this.onRetry,
    this.onDelete,
  });

  final MediaItemPreview item;
  final VoidCallback? onTap;
  final VoidCallback? onRetry;
  final VoidCallback? onDelete;

  @override
  Widget build(BuildContext context) {
    final status = item.status;
    final color = status.color(context);
    final scheme = Theme.of(context).colorScheme;
    final showRetry = status == MediaStatus.failed;
    final showProgress = status == MediaStatus.downloading ||
        status == MediaStatus.processing;

    return Card(
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _StatusGlyph(status: status, color: color),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: Text(
                                item.displayTitle,
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                                style: Theme.of(context).textTheme.titleSmall,
                              ),
                            ),
                            PlatformBadge(platform: item.platform, compact: true),
                          ],
                        ),
                        const SizedBox(height: 4),
                        Text(
                          status.label,
                          style: Theme.of(context).textTheme.labelMedium?.copyWith(
                                color: color,
                                fontWeight: FontWeight.w700,
                              ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          item.errorMessage ?? status.description,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                                color: scheme.onSurface.withValues(alpha: 0.65),
                              ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              if (showProgress) ...[
                const SizedBox(height: 12),
                ClipRRect(
                  borderRadius: BorderRadius.circular(999),
                  child: LinearProgressIndicator(
                    value: item.progressPercent != null
                        ? item.progressPercent! / 100
                        : null,
                    minHeight: 6,
                    color: color,
                    backgroundColor: color.withValues(alpha: 0.15),
                  ),
                ),
                if (item.progressPercent != null) ...[
                  const SizedBox(height: 6),
                  Text(
                    '${item.progressPercent}%',
                    style: Theme.of(context).textTheme.labelSmall?.copyWith(
                          color: scheme.onSurface.withValues(alpha: 0.6),
                        ),
                  ),
                ],
              ],
              if (showRetry || onDelete != null) ...[
                const SizedBox(height: 12),
                Row(
                  children: [
                    if (showRetry)
                      FilledButton.tonalIcon(
                        onPressed: onRetry,
                        icon: const Icon(Icons.refresh_rounded, size: 18),
                        label: const Text('Retry'),
                      ),
                    if (showRetry && onDelete != null) const SizedBox(width: 8),
                    if (onDelete != null)
                      OutlinedButton.icon(
                        onPressed: onDelete,
                        icon: const Icon(Icons.delete_outline_rounded, size: 18),
                        label: const Text('Delete'),
                      ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _StatusGlyph extends StatelessWidget {
  const _StatusGlyph({required this.status, required this.color});

  final MediaStatus status;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return AnimatedContainer(
      duration: const Duration(milliseconds: 280),
      curve: Curves.easeOutCubic,
      width: 44,
      height: 44,
      decoration: BoxDecoration(
        color: status.containerColor(context),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Icon(status.icon, color: color, size: 22),
    );
  }
}
