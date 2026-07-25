import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';

import '../../../../shared/data/ui_placeholder_catalog.dart';
import '../../../../shared/models/media_status.dart';
import '../../../../shared/models/media_status_ui.dart';
import '../../../../shared/widgets/media_thumbnail_placeholder.dart';
import '../../../../shared/widgets/platform_badge.dart';

/// Media detail screen (SRS §7 / FR-014–016 UI shell).
class MediaDetailPage extends StatelessWidget {
  const MediaDetailPage({super.key, required this.mediaId});

  final String mediaId;

  @override
  Widget build(BuildContext context) {
    final item = UiPlaceholderCatalog.byId(mediaId);
    final scheme = Theme.of(context).colorScheme;
    final canPlay = item.status == MediaStatus.completed;
    final canRetry = item.status == MediaStatus.failed;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Media detail'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: () {
            if (context.canPop()) {
              context.pop();
            } else {
              context.go('/library');
            }
          },
        ),
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
        children: [
          MediaThumbnailPlaceholder(
            aspectRatio: 16 / 10,
            borderRadius: 18,
            showPlayHint: canPlay,
          ),
          const SizedBox(height: 16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.displayTitle,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      PlatformBadge(platform: item.platform),
                      const SizedBox(width: 10),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                        decoration: BoxDecoration(
                          color: item.status.containerColor(context),
                          borderRadius: BorderRadius.circular(999),
                        ),
                        child: Text(
                          item.status.label,
                          style: Theme.of(context).textTheme.labelMedium?.copyWith(
                                color: item.status.color(context),
                                fontWeight: FontWeight.w700,
                              ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 18),
                  _MetaRow(
                    label: 'Original URL',
                    value: item.originalUrl,
                    trailing: IconButton(
                      tooltip: 'Copy URL',
                      onPressed: () async {
                        await Clipboard.setData(ClipboardData(text: item.originalUrl));
                        if (context.mounted) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(content: Text('URL copied.')),
                          );
                        }
                      },
                      icon: const Icon(Icons.copy_rounded, size: 18),
                    ),
                  ),
                  const Divider(height: 24),
                  _MetaRow(
                    label: 'Saved date',
                    value: _formatDateTime(item.savedAt),
                  ),
                  const Divider(height: 24),
                  _MetaRow(
                    label: 'File size',
                    value: item.fileSizeLabel ?? 'Not available yet',
                  ),
                  if (item.errorMessage != null) ...[
                    const Divider(height: 24),
                    _MetaRow(
                      label: 'Error',
                      value: item.errorMessage!,
                      valueColor: scheme.error,
                    ),
                  ],
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Text(
            'Video placeholder',
            style: Theme.of(context).textTheme.titleSmall?.copyWith(
                  color: scheme.onSurface.withValues(alpha: 0.65),
                ),
          ),
          const SizedBox(height: 8),
          MediaThumbnailPlaceholder(
            aspectRatio: 9 / 16,
            borderRadius: 18,
            showPlayHint: true,
          ),
          const SizedBox(height: 24),
          FilledButton.icon(
            onPressed: canPlay
                ? () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text(
                          'Playback will use GET /media/{id}/playback in a later sprint.',
                        ),
                      ),
                    );
                  }
                : null,
            icon: const Icon(Icons.play_arrow_rounded),
            label: const Text('Play'),
          ),
          const SizedBox(height: 10),
          if (canRetry)
            FilledButton.tonalIcon(
              onPressed: () {
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(
                    content: Text('Retry will call POST /media/{id}/retry later.'),
                  ),
                );
              },
              icon: const Icon(Icons.refresh_rounded),
              label: const Text('Retry'),
            ),
          if (canRetry) const SizedBox(height: 10),
          OutlinedButton.icon(
            onPressed: () {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Delete will call DELETE /media/{id} later.'),
                ),
              );
            },
            icon: Icon(Icons.delete_outline_rounded, color: scheme.error),
            label: Text('Delete', style: TextStyle(color: scheme.error)),
          ),
        ],
      ),
    );
  }

  String _formatDateTime(DateTime value) {
    final local = value.toLocal();
    final y = local.year.toString().padLeft(4, '0');
    final m = local.month.toString().padLeft(2, '0');
    final d = local.day.toString().padLeft(2, '0');
    final hh = local.hour.toString().padLeft(2, '0');
    final mm = local.minute.toString().padLeft(2, '0');
    return '$y-$m-$d  $hh:$mm';
  }
}

class _MetaRow extends StatelessWidget {
  const _MetaRow({
    required this.label,
    required this.value,
    this.trailing,
    this.valueColor,
  });

  final String label;
  final String value;
  final Widget? trailing;
  final Color? valueColor;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: Theme.of(context).textTheme.labelMedium?.copyWith(
                      color: scheme.onSurface.withValues(alpha: 0.55),
                    ),
              ),
              const SizedBox(height: 4),
              SelectableText(
                value,
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                      color: valueColor,
                    ),
              ),
            ],
          ),
        ),
        ? trailing,
      ],
    );
  }
}
