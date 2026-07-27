import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme/app_colors.dart';
import '../../core/theme/app_spacing.dart';

/// Shared circular glass back button (Media Detail, Share Entry, etc.).
class AppBackButton extends StatelessWidget {
  const AppBackButton({
    super.key,
    this.onPressed,
  });

  /// Optional override; defaults to [GoRouter] pop / [Navigator.maybePop].
  final VoidCallback? onPressed;

  static const double size = 34;
  static const double iconSize = 18;
  static const double topInset = AppSpacing.xs;
  static const double gapBelow = AppSpacing.md;

  static double horizontalInset(BuildContext context) {
    return AppSpacing.horizontalInset(context);
  }

  void _handlePress(BuildContext context) {
    if (onPressed != null) {
      onPressed!();
      return;
    }
    if (context.canPop()) {
      context.pop();
    } else {
      Navigator.of(context).maybePop();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: () => _handlePress(context),
        customBorder: const CircleBorder(),
        child: Ink(
          width: size,
          height: size,
          decoration: BoxDecoration(
            shape: BoxShape.circle,
            color: AppColors.glassFill,
            border: Border.all(
              color: AppColors.glassBorder,
              width: 1,
            ),
          ),
          child: const Icon(
            Icons.arrow_back_rounded,
            size: iconSize,
            color: AppColors.splashTextPrimary,
          ),
        ),
      ),
    );
  }
}

/// Top-left [AppBackButton] with the shared screen-edge insets.
class AppBackButtonHeader extends StatelessWidget {
  const AppBackButtonHeader({
    super.key,
    this.onPressed,
  });

  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    final inset = AppBackButton.horizontalInset(context);
    return Padding(
      padding: EdgeInsets.only(
        left: inset,
        top: AppBackButton.topInset,
        right: inset,
      ),
      child: Align(
        alignment: Alignment.centerLeft,
        child: AppBackButton(onPressed: onPressed),
      ),
    );
  }
}
