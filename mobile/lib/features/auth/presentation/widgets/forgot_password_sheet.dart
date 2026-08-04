import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/errors/app_exception.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../providers/auth_providers.dart';
import 'login_brand_mark.dart';
import 'login_glass_field.dart';

/// Opens a 3-step forgot / reset password sheet. Returns `true` when reset
/// succeeds so the login screen can show a confirmation snackbar.
Future<bool?> showForgotPasswordSheet(
  BuildContext context, {
  String? initialEmail,
}) {
  return showModalBottomSheet<bool>(
    context: context,
    isScrollControlled: true,
    backgroundColor: Colors.transparent,
    builder: (context) => ForgotPasswordSheet(initialEmail: initialEmail),
  );
}

class ForgotPasswordSheet extends ConsumerStatefulWidget {
  const ForgotPasswordSheet({super.key, this.initialEmail});

  final String? initialEmail;

  @override
  ConsumerState<ForgotPasswordSheet> createState() =>
      _ForgotPasswordSheetState();
}

class _ForgotPasswordSheetState extends ConsumerState<ForgotPasswordSheet> {
  final _emailController = TextEditingController();
  final _otpController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmController = TextEditingController();

  var _step = 0;
  var _isSubmitting = false;
  var _obscurePassword = true;
  var _obscureConfirm = true;
  String? _error;
  String? _info;

  @override
  void initState() {
    super.initState();
    final seed = widget.initialEmail?.trim();
    if (seed != null && seed.isNotEmpty) {
      _emailController.text = seed;
    }
  }

  @override
  void dispose() {
    _emailController.dispose();
    _otpController.dispose();
    _passwordController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  String? _validateEmail(String email) {
    if (email.isEmpty) return 'Enter your email.';
    final pattern = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
    if (!pattern.hasMatch(email)) return 'Enter a valid email address.';
    return null;
  }

  String? _validateOtp(String otp) {
    if (otp.isEmpty) return 'Enter the 6-digit code.';
    if (!RegExp(r'^\d{6}$').hasMatch(otp)) {
      return 'OTP must be a 6-digit code.';
    }
    return null;
  }

  String? _validatePassword(String password) {
    if (password.isEmpty) return 'Enter a new password.';
    if (password.length < 8) {
      return 'Password must be at least 8 characters.';
    }
    if (!RegExp(r'[A-Z]').hasMatch(password) ||
        !RegExp(r'[a-z]').hasMatch(password) ||
        !RegExp(r'[0-9]').hasMatch(password)) {
      return 'Use upper, lower, and a number.';
    }
    return null;
  }

  Future<void> _sendCode() async {
    FocusScope.of(context).unfocus();
    final email = _emailController.text.trim();
    final emailError = _validateEmail(email);
    if (emailError != null) {
      setState(() {
        _error = emailError;
        _info = null;
      });
      return;
    }

    setState(() {
      _isSubmitting = true;
      _error = null;
      _info = null;
    });

    try {
      final message = await ref.read(forgotPasswordUseCaseProvider)(
        email: email,
      );
      if (!mounted) return;
      setState(() {
        _step = 1;
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
        _error = 'Could not send reset code. Try again.';
        _isSubmitting = false;
      });
    }
  }

  Future<void> _continueToPassword() async {
    FocusScope.of(context).unfocus();
    final otpError = _validateOtp(_otpController.text.trim());
    if (otpError != null) {
      setState(() {
        _error = otpError;
        _info = null;
      });
      return;
    }
    setState(() {
      _step = 2;
      _error = null;
      _info = null;
    });
  }

  Future<void> _resetPassword() async {
    FocusScope.of(context).unfocus();
    final email = _emailController.text.trim();
    final otp = _otpController.text.trim();
    final password = _passwordController.text;
    final confirm = _confirmController.text;

    final passwordError = _validatePassword(password);
    if (passwordError != null) {
      setState(() {
        _error = passwordError;
        _info = null;
      });
      return;
    }
    if (password != confirm) {
      setState(() {
        _error = 'Passwords do not match.';
        _info = null;
      });
      return;
    }

    setState(() {
      _isSubmitting = true;
      _error = null;
      _info = null;
    });

    try {
      await ref.read(resetPasswordUseCaseProvider)(
        email: email,
        otp: otp,
        newPassword: password,
      );
      if (!mounted) return;
      Navigator.of(context).pop(true);
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _isSubmitting = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error = 'Could not reset password. Try again.';
        _isSubmitting = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.viewInsetsOf(context).bottom;
    final safeBottom = MediaQuery.paddingOf(context).bottom;

    final titles = ['Forgot password', 'Enter code', 'New password'];
    final subtitles = [
      'We will email a 6-digit reset code to your ReelBox account.',
      'Check your inbox for the code. It expires in 15 minutes.',
      'Choose a new password, then sign in again.',
    ];

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
            Text(
              titles[_step],
              style: const TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w700,
                color: AppColors.splashTextPrimary,
              ),
            ),
            const SizedBox(height: AppSpacing.xs),
            Text(
              subtitles[_step],
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
            if (_step == 0) ...[
              LoginGlassField(
                controller: _emailController,
                label: 'Email',
                hint: 'you@email.com',
                prefixIcon: Icons.mail_outline_rounded,
                keyboardType: TextInputType.emailAddress,
                textInputAction: TextInputAction.done,
                autofillHints: const [AutofillHints.email],
                autocorrect: false,
                onFieldSubmitted: (_) {
                  if (!_isSubmitting) _sendCode();
                },
              ),
              const SizedBox(height: AppSpacing.lg),
              LoginGradientButton(
                label: _isSubmitting ? 'Sending…' : 'Send code',
                onPressed: _isSubmitting ? null : _sendCode,
              ),
            ] else if (_step == 1) ...[
              LoginGlassField(
                controller: _otpController,
                label: 'Reset code',
                hint: '6-digit code',
                prefixIcon: Icons.pin_outlined,
                keyboardType: TextInputType.number,
                textInputAction: TextInputAction.done,
                autocorrect: false,
                onFieldSubmitted: (_) {
                  if (!_isSubmitting) _continueToPassword();
                },
              ),
              const SizedBox(height: AppSpacing.sm),
              Align(
                alignment: Alignment.centerLeft,
                child: TextButton(
                  onPressed: _isSubmitting
                      ? null
                      : () {
                          setState(() {
                            _step = 0;
                            _error = null;
                            _info = null;
                          });
                        },
                  style: TextButton.styleFrom(
                    foregroundColor: AppColors.splashTextMuted,
                    padding: EdgeInsets.zero,
                    minimumSize: Size.zero,
                    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  ),
                  child: const Text('Change email'),
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              LoginGradientButton(
                label: 'Continue',
                onPressed: _isSubmitting ? null : _continueToPassword,
              ),
              const SizedBox(height: AppSpacing.sm),
              TextButton(
                onPressed: _isSubmitting ? null : _sendCode,
                style: TextButton.styleFrom(
                  foregroundColor: AppColors.splashTextMuted,
                ),
                child: const Text('Resend code'),
              ),
            ] else ...[
              LoginGlassField(
                controller: _passwordController,
                label: 'New password',
                hint: '••••••••',
                prefixIcon: Icons.lock_outline_rounded,
                obscureText: _obscurePassword,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.newPassword],
                autocorrect: false,
                suffixIcon: IconButton(
                  tooltip: _obscurePassword ? 'Show password' : 'Hide password',
                  onPressed: () {
                    setState(() => _obscurePassword = !_obscurePassword);
                  },
                  icon: Icon(
                    _obscurePassword
                        ? Icons.visibility_outlined
                        : Icons.visibility_off_outlined,
                    color: AppColors.splashTextMuted,
                    size: 20,
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.md),
              LoginGlassField(
                controller: _confirmController,
                label: 'Confirm password',
                hint: '••••••••',
                prefixIcon: Icons.lock_outline_rounded,
                obscureText: _obscureConfirm,
                textInputAction: TextInputAction.done,
                autofillHints: const [AutofillHints.newPassword],
                autocorrect: false,
                onFieldSubmitted: (_) {
                  if (!_isSubmitting) _resetPassword();
                },
                suffixIcon: IconButton(
                  tooltip: _obscureConfirm ? 'Show password' : 'Hide password',
                  onPressed: () {
                    setState(() => _obscureConfirm = !_obscureConfirm);
                  },
                  icon: Icon(
                    _obscureConfirm
                        ? Icons.visibility_outlined
                        : Icons.visibility_off_outlined,
                    color: AppColors.splashTextMuted,
                    size: 20,
                  ),
                ),
              ),
              const SizedBox(height: AppSpacing.lg),
              LoginGradientButton(
                label: _isSubmitting ? 'Updating…' : 'Reset password',
                onPressed: _isSubmitting ? null : _resetPassword,
              ),
            ],
          ],
        ),
      ),
    );
  }
}
