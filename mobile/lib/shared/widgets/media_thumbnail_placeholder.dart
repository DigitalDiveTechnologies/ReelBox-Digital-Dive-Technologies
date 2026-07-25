import 'package:flutter/material.dart';

/// Neutral thumbnail placeholder — no random images (Sprint F1).
class MediaThumbnailPlaceholder extends StatelessWidget {
  const MediaThumbnailPlaceholder({
    super.key,
    this.aspectRatio = 9 / 16,
    this.borderRadius = 14,
    this.showPlayHint = false,
    this.compact = false,
  });

  final double aspectRatio;
  final double borderRadius;
  final bool showPlayHint;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return AspectRatio(
      aspectRatio: aspectRatio,
      child: DecoratedBox(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(borderRadius),
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [
              scheme.primary.withValues(alpha: 0.18),
              scheme.tertiary.withValues(alpha: 0.22),
              scheme.surfaceContainerHighest,
            ],
          ),
          border: Border.all(color: scheme.outline.withValues(alpha: 0.25)),
        ),
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                showPlayHint ? Icons.play_circle_outline_rounded : Icons.movie_outlined,
                size: compact ? 28 : 40,
                color: scheme.onSurface.withValues(alpha: 0.55),
              ),
              if (!compact) ...[
                const SizedBox(height: 8),
                Text(
                  showPlayHint ? 'Video placeholder' : 'Thumbnail',
                  style: Theme.of(context).textTheme.labelMedium?.copyWith(
                        color: scheme.onSurface.withValues(alpha: 0.55),
                      ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
