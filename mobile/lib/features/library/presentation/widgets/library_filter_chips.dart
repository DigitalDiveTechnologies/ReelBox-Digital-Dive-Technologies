import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/models/media_platform.dart';
import '../../../../shared/models/reel_category.dart';

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
          LibraryFilterChip(
            label: 'All',
            selected: selected == null,
            onTap: () => onSelected(null),
          ),
          const SizedBox(width: AppSpacing.sm),
          LibraryFilterChip(
            label: 'Instagram',
            selected: selected == MediaPlatform.instagram,
            onTap: () => onSelected(MediaPlatform.instagram),
          ),
          const SizedBox(width: AppSpacing.sm),
          LibraryFilterChip(
            label: 'Facebook',
            selected: selected == MediaPlatform.facebook,
            onTap: () => onSelected(MediaPlatform.facebook),
          ),
        ],
      ),
    );
  }
}

/// Category filter chips — sleeker pills + trailing scroll hint.
class LibraryCategoryFilterChips extends StatelessWidget {
  const LibraryCategoryFilterChips({
    super.key,
    required this.selected,
    required this.onSelected,
  });

  /// Full backend category name, or null for All.
  final String? selected;
  final ValueChanged<String?> onSelected;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: [
                for (var i = 0; i < ReelCategory.filterOptions.length; i++) ...[
                  if (i > 0) const SizedBox(width: AppSpacing.xs),
                  Builder(
                    builder: (_) {
                      final option = ReelCategory.filterOptions[i];
                      final value = option.value;
                      return LibraryFilterChip(
                        label: option.label,
                        selected: selected == value,
                        onTap: () => onSelected(value),
                        compact: true,
                      );
                    },
                  ),
                ],
              ],
            ),
          ),
        ),
        const SizedBox(width: AppSpacing.xxs),
        IgnorePointer(
          child: Icon(
            Icons.chevron_right_rounded,
            size: 18,
            color: AppColors.splashTextMuted.withValues(alpha: 0.85),
          ),
        ),
      ],
    );
  }
}

/// Shared Library filter chip (platform + category).
class LibraryFilterChip extends StatelessWidget {
  const LibraryFilterChip({
    super.key,
    required this.label,
    required this.selected,
    required this.onTap,
    this.compact = false,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    final radius = compact ? BorderRadius.circular(14) : AppRadius.circularPill;
    final padding = compact
        ? const EdgeInsets.symmetric(horizontal: 12, vertical: 6)
        : const EdgeInsets.symmetric(
            horizontal: AppSpacing.lg,
            vertical: AppSpacing.sm,
          );
    final fontSize = compact ? 12.0 : 13.0;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: radius,
        child: Ink(
          padding: padding,
          decoration: BoxDecoration(
            borderRadius: radius,
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
            constraints: const BoxConstraints(minWidth: 40),
            child: Text(
              label,
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: fontSize,
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
