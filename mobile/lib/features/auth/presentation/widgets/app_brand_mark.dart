import 'package:flutter/material.dart';

/// Reusable brand mark used on Splash and other auth surfaces.
class AppBrandMark extends StatelessWidget {
  const AppBrandMark({
    super.key,
    this.size = 88,
    this.iconSize = 44,
    this.borderRadius = 28,
  });

  final double size;
  final double iconSize;
  final double borderRadius;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        color: scheme.primary,
        borderRadius: BorderRadius.circular(borderRadius),
        boxShadow: [
          BoxShadow(
            color: scheme.primary.withValues(alpha: 0.28),
            blurRadius: 24,
            offset: const Offset(0, 10),
          ),
        ],
      ),
      child: Icon(
        Icons.video_collection_rounded,
        size: iconSize,
        color: scheme.onPrimary,
      ),
    );
  }
}
