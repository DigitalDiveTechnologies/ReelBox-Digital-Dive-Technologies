import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/models/media_platform.dart';

/// Platform filter chips matching the Library mockup (All / Instagram / Facebook).
class LibraryFilterChips extends StatelessWidget {
  const LibraryFilterChips({
    super.key,
    required this.selected,
    required this.onSelected,
  });

  final MediaPlatform? selected;
  final ValueChanged<MediaPlatform?> onSelected;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          _Chip(
            label: 'All',
            selected: selected == null,
            onTap: () => onSelected(null),
          ),
          const SizedBox(width: AppSpacing.sm),
          _Chip(
            label: 'Instagram',
            selected: selected == MediaPlatform.instagram,
            onTap: () => onSelected(MediaPlatform.instagram),
          ),
          const SizedBox(width: AppSpacing.sm),
          _Chip(
            label: 'Facebook',
            selected: selected == MediaPlatform.facebook,
            onTap: () => onSelected(MediaPlatform.facebook),
          ),
        ],
      ),
    );
  }
}

class _Chip extends StatelessWidget {
  const _Chip({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: AppRadius.circularPill,
        child: Ink(
          padding: const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.sm,
          ),
          decoration: BoxDecoration(
            borderRadius: AppRadius.circularPill,
            gradient: selected ? AppGradients.brandCta : null,
            color: selected
                ? null
                : AppColors.splashChipFill.withValues(alpha: 0.55),
            border: selected
                ? null
                : Border.all(
                    color: AppColors.splashChipBorder.withValues(alpha: 0.9),
                  ),
          ),
          child: ConstrainedBox(
            constraints: const BoxConstraints(minWidth: 48),
            child: Text(
              label,
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 13,
                fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
                color: selected
                    ? AppColors.splashTextPrimary
                    : AppColors.splashTextMuted,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
