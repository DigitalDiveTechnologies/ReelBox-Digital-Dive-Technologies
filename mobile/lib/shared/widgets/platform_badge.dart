import 'package:flutter/material.dart';

import '../models/media_platform.dart';

/// Compact platform badge for library and download cards (SRS §7).
class PlatformBadge extends StatelessWidget {
  const PlatformBadge({
    super.key,
    required this.platform,
    this.compact = false,
  });

  final MediaPlatform platform;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final isIg = platform == MediaPlatform.instagram;

    return Container(
      padding: EdgeInsets.symmetric(
        horizontal: compact ? 8 : 10,
        vertical: compact ? 4 : 6,
      ),
      decoration: BoxDecoration(
        color: isIg
            ? scheme.tertiaryContainer.withValues(alpha: 0.55)
            : scheme.secondaryContainer.withValues(alpha: 0.65),
        borderRadius: BorderRadius.circular(999),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            isIg ? Icons.camera_alt_outlined : Icons.public_outlined,
            size: compact ? 12 : 14,
            color: scheme.onSurface.withValues(alpha: 0.85),
          ),
          const SizedBox(width: 4),
          Text(
            compact ? platform.shortLabel : platform.label,
            style: Theme.of(context).textTheme.labelSmall?.copyWith(
                  fontWeight: FontWeight.w600,
                  color: scheme.onSurface.withValues(alpha: 0.9),
                ),
          ),
        ],
      ),
    );
  }
}
