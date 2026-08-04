import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/errors/app_exception.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../domain/entities/auth_user.dart';
import '../providers/auth_providers.dart';
import 'login_brand_mark.dart';
import 'login_glass_field.dart';

/// Signup email OTP sheet. Returns [AuthUser] when verification succeeds.
Future<AuthUser?> showSignupOtpSheet(
  BuildContext context, {
  required String email,
  String? infoMessage,
}) {
  return showModalBottomSheet<AuthUser>(
    context: context,
    isScrollControlled: true,
    backgroundColor: Colors.transparent,
    isDismissible: false,
    enableDrag: false,
    builder: (context) => SignupOtpSheet(
      email: email,
      infoMessage: infoMessage,
    ),
  );
}

class SignupOtpSheet extends ConsumerStatefulWidget {
  const SignupOtpSheet({
    super.key,
    required this.email,
    this.infoMessage,
  });

  final String email;
  final String? infoMessage;

  @override
  ConsumerState<SignupOtpSheet> createState() => _SignupOtpSheetState();
}

class _SignupOtpSheetState extends ConsumerState<SignupOtpSheet> {
  final _otpController = TextEditingController();
  var _isSubmitting = false;
  String? _error;
  String? _info;

  @override
  void initState() {
    super.initState();
    _info = widget.infoMessage;
  }

  @override
  void dispose() {
    _otpController.dispose();
    super.dispose();
  }

  Future<void> _verify() async {
    FocusScope.of(context).unfocus();
    final otp = _otpController.text.trim();
    if (!RegExp(r'^\d{6}$').hasMatch(otp)) {
      setState(() => _error = 'Enter the 6-digit code from your email.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _error = null;
    });

    try {
      final user = await ref.read(authNotifierProvider.notifier).verifySignupOtp(
            email: widget.email,
            otp: otp,
          );
      if (!mounted) return;
      Navigator.of(context).pop(user);
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _isSubmitting = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error = 'Could not verify code. Try again.';
        _isSubmitting = false;
      });
    }
  }

  Future<void> _resend() async {
    setState(() {
      _isSubmitting = true;
      _error = null;
    });
    try {
      final message = await ref.read(resendSignupOtpUseCaseProvider)(
        email: widget.email,
      );
      if (!mounted) return;
      setState(() {
        _info = message;
        _isSubmitting = false;
      });
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _isSubmitting = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error = 'Could not resend code. Try again.';
        _isSubmitting = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.viewInsetsOf(context).bottom;
    final safeBottom = MediaQuery.paddingOf(context).bottom;

    return Padding(
      padding: EdgeInsets.only(bottom: bottomInset),
      child: Container(
        width: double.infinity,
        decoration: BoxDecoration(
          color: AppColors.splashSheet,
          borderRadius: AppRadius.sheetTop,
        ),
        padding: EdgeInsets.fromLTRB(
          AppSpacing.xl,
          AppSpacing.md,
          AppSpacing.xl,
          AppSpacing.xl + safeBottom,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                width: AppSpacing.splashHandleWidth,
                height: AppSpacing.splashHandleHeight,
                decoration: BoxDecoration(
                  color: AppColors.splashHandle.withValues(alpha: 0.85),
                  borderRadius: AppRadius.circularPill,
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            const Text(
              'Verify email',
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w700,
                color: AppColors.splashTextPrimary,
              ),
            ),
            const SizedBox(height: AppSpacing.xs),
            Text(
              'Enter the 6-digit code sent to ${widget.email}',
              style: TextStyle(
                fontSize: 13,
                color: AppColors.splashTextMuted.withValues(alpha: 0.95),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            if (_info != null) ...[
              Text(
                _info!,
                style: TextStyle(
                  fontSize: 13,
                  color: AppColors.splashTextMuted.withValues(alpha: 0.95),
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
            ],
            if (_error != null) ...[
              Text(
                _error!,
                style: const TextStyle(
                  fontSize: 13,
                  color: AppColors.statusFailed,
                ),
              ),
              const SizedBox(height: AppSpacing.sm),
            ],
            LoginGlassField(
              controller: _otpController,
              label: 'Verification code',
              hint: '6-digit code',
              prefixIcon: Icons.pin_outlined,
              keyboardType: TextInputType.number,
              textInputAction: TextInputAction.done,
              autocorrect: false,
              onFieldSubmitted: (_) {
                if (!_isSubmitting) _verify();
              },
            ),
            const SizedBox(height: AppSpacing.lg),
            LoginGradientButton(
              label: _isSubmitting ? 'Verifying…' : 'Verify & continue',
              onPressed: _isSubmitting ? null : _verify,
            ),
            const SizedBox(height: AppSpacing.sm),
            TextButton(
              onPressed: _isSubmitting ? null : _resend,
              style: TextButton.styleFrom(
                foregroundColor: AppColors.splashTextMuted,
              ),
              child: const Text('Resend code'),
            ),
          ],
        ),
      ),
    );
  }
}
