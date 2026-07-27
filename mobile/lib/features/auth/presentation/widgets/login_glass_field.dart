import 'dart:ui';

import 'package:flutter/material.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';

/// Glass auth field styled for the Login mockup (label above + bordered input).
class LoginGlassField extends StatelessWidget {
  const LoginGlassField({
    super.key,
    required this.controller,
    required this.label,
    required this.prefixIcon,
    this.focusNode,
    this.hint,
    this.obscureText = false,
    this.keyboardType,
    this.textInputAction,
    this.autofillHints,
    this.validator,
    this.onFieldSubmitted,
    this.suffixIcon,
    this.autocorrect = true,
  });

  final TextEditingController controller;
  final FocusNode? focusNode;
  final String label;
  final IconData prefixIcon;
  final String? hint;
  final bool obscureText;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final Iterable<String>? autofillHints;
  final FormFieldValidator<String>? validator;
  final ValueChanged<String>? onFieldSubmitted;
  final Widget? suffixIcon;
  final bool autocorrect;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w400,
            letterSpacing: 0.1,
            color: AppColors.splashTextMuted.withValues(alpha: 0.95),
          ),
        ),
        const SizedBox(height: AppSpacing.xs),
        ClipRRect(
          borderRadius: AppRadius.circularCard,
          child: BackdropFilter(
            filter: ImageFilter.blur(sigmaX: 10, sigmaY: 10),
            child: TextFormField(
              controller: controller,
              focusNode: focusNode,
              obscureText: obscureText,
              keyboardType: keyboardType,
              textInputAction: textInputAction,
              autofillHints: autofillHints,
              validator: validator,
              onFieldSubmitted: onFieldSubmitted,
              autocorrect: autocorrect,
              style: const TextStyle(
                fontSize: 15,
                fontWeight: FontWeight.w400,
                color: AppColors.splashTextPrimary,
              ),
              cursorColor: AppColors.brandOrange,
              decoration: InputDecoration(
                hintText: hint,
                hintStyle: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w400,
                  color: AppColors.splashTextMuted.withValues(alpha: 0.75),
                ),
                prefixIcon: Icon(
                  prefixIcon,
                  size: 20,
                  color: AppColors.splashTextMuted,
                ),
                suffixIcon: suffixIcon,
                filled: true,
                fillColor: AppColors.splashChipFill.withValues(alpha: 0.55),
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.cardPadding,
                  vertical: AppSpacing.md,
                ),
                border: OutlineInputBorder(
                  borderRadius: AppRadius.circularCard,
                  borderSide: BorderSide(
                    color: AppColors.splashChipBorder.withValues(alpha: 0.95),
                  ),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: AppRadius.circularCard,
                  borderSide: BorderSide(
                    color: AppColors.splashChipBorder.withValues(alpha: 0.95),
                  ),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: AppRadius.circularCard,
                  borderSide: const BorderSide(
                    color: AppColors.brandPurple,
                    width: 1.4,
                  ),
                ),
                errorBorder: OutlineInputBorder(
                  borderRadius: AppRadius.circularCard,
                  borderSide: const BorderSide(color: AppColors.statusFailed),
                ),
                focusedErrorBorder: OutlineInputBorder(
                  borderRadius: AppRadius.circularCard,
                  borderSide: const BorderSide(
                    color: AppColors.statusFailed,
                    width: 1.4,
                  ),
                ),
                errorStyle: const TextStyle(
                  color: AppColors.statusFailed,
                  fontSize: 12,
                ),
              ),
            ),
          ),
        ),
      ],
    );
  }
}
