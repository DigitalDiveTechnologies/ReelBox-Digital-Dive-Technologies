import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_exception.dart';
import '../../../../core/router/route_paths.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_shadows.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../media/presentation/providers/media_providers.dart';
import '../../../share/presentation/providers/pending_share_provider.dart';
import '../providers/auth_providers.dart';
import '../widgets/login_brand_mark.dart';
import '../widgets/login_glass_field.dart';
import '../widgets/signup_otp_sheet.dart';

/// Register screen — account creation entry (SRS §7 / §16 / §22).
///
/// Presentation is design-locked to the approved Register mockup.
class RegisterPage extends ConsumerStatefulWidget {
  const RegisterPage({super.key});

  @override
  ConsumerState<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends ConsumerState<RegisterPage> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  final _nameFocus = FocusNode();
  final _emailFocus = FocusNode();
  final _passwordFocus = FocusNode();

  var _obscurePassword = true;
  var _submitted = false;
  var _isSubmitting = false;

  @override
  void dispose() {
    _nameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _nameFocus.dispose();
    _emailFocus.dispose();
    _passwordFocus.dispose();
    super.dispose();
  }

  String? _validateName(String? value) {
    final name = value?.trim() ?? '';
    if (name.isEmpty) {
      return 'Enter a display name.';
    }
    if (name.length < 2) {
      return 'Display name must be at least 2 characters.';
    }
    return null;
  }

  String? _validateEmail(String? value) {
    final email = value?.trim() ?? '';
    if (email.isEmpty) {
      return 'Enter your email.';
    }
    final emailPattern = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');
    if (!emailPattern.hasMatch(email)) {
      return 'Enter a valid email address.';
    }
    return null;
  }

  String? _validatePassword(String? value) {
    final password = value ?? '';
    if (password.isEmpty) {
      return 'Enter a password.';
    }
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

  Future<void> _onCreateAccount() async {
    FocusScope.of(context).unfocus();
    setState(() => _submitted = true);

    final isValid = _formKey.currentState?.validate() ?? false;
    if (!isValid) return;

    setState(() => _isSubmitting = true);
    try {
      final email = _emailController.text.trim();
      final message = await ref.read(authNotifierProvider.notifier).register(
            email: email,
            password: _passwordController.text,
          );
      if (!mounted) return;

      final user = await showSignupOtpSheet(
        context,
        email: email,
        infoMessage: message,
      );
      if (!mounted) return;
      if (user == null) return;

      try {
        ref.invalidate(mediaListProvider);
        await ref.read(mediaListProvider.future);
      } catch (_) {
        // Signup succeeded; Home will retry via its existing error handling.
      }
      if (!mounted) return;

      final pendingShareUrl = ref.read(pendingShareUrlProvider);
      if (pendingShareUrl != null && pendingShareUrl.trim().isNotEmpty) {
        context.go(shareRouteForUrl(pendingShareUrl.trim()));
      } else {
        context.go(RoutePaths.home);
      }
    } on AppException catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(error.message)),
      );
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not create account. Try again.')),
      );
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  void _onLogIn() {
    context.go(RoutePaths.login);
  }

  @override
  Widget build(BuildContext context) {
    final horizontal = AppSpacing.horizontalInset(context);
    final topPad = MediaQuery.sizeOf(context).height < 700
        ? AppSpacing.xl
        : AppSpacing.huge;

    return Scaffold(
      backgroundColor: AppColors.splashBgDeep,
      body: Stack(
        fit: StackFit.expand,
        children: [
          const DecoratedBox(
            decoration: BoxDecoration(gradient: AppGradients.splashBackground),
          ),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: const Alignment(0, -0.9),
                radius: 1.0,
                colors: [
                  AppColors.splashBgMahogany.withValues(alpha: 0.55),
                  AppColors.splashBgMahogany.withValues(alpha: 0),
                ],
              ),
            ),
          ),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: const Alignment(0.8, 0.95),
                radius: 0.9,
                colors: [
                  AppColors.splashBgNavy.withValues(alpha: 0.45),
                  AppColors.splashBgNavy.withValues(alpha: 0),
                ],
              ),
            ),
          ),
          SafeArea(
            child: Center(
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 440),
                child: AutofillGroup(
                  child: Form(
                    key: _formKey,
                    autovalidateMode: _submitted
                        ? AutovalidateMode.onUserInteraction
                        : AutovalidateMode.disabled,
                    child: ListView(
                      padding: EdgeInsets.fromLTRB(
                        horizontal,
                        topPad,
                        horizontal,
                        AppSpacing.xxl,
                      ),
                      children: [
                        const Center(child: LoginBrandMark()),
                        const SizedBox(height: AppSpacing.section),
                        const Text(
                          'Create account',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontSize: 28,
                            fontWeight: FontWeight.w700,
                            letterSpacing: -0.4,
                            height: 1.15,
                            color: AppColors.splashTextPrimary,
                          ),
                        ),
                        const SizedBox(height: AppSpacing.xs),
                        Text(
                          'Start saving reels in seconds',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w400,
                            letterSpacing: 0.1,
                            height: 1.4,
                            color: AppColors.splashTextMuted.withValues(
                              alpha: 0.95,
                            ),
                          ),
                        ),
                        const SizedBox(height: AppSpacing.section),
                        LoginGlassField(
                          controller: _nameController,
                          focusNode: _nameFocus,
                          label: 'Name',
                          hint: 'Your name',
                          prefixIcon: Icons.person_outline_rounded,
                          textInputAction: TextInputAction.next,
                          autofillHints: const [AutofillHints.name],
                          validator: _validateName,
                          onFieldSubmitted: (_) => _emailFocus.requestFocus(),
                        ),
                        const SizedBox(height: AppSpacing.md),
                        LoginGlassField(
                          controller: _emailController,
                          focusNode: _emailFocus,
                          label: 'Email',
                          hint: 'name@email.com',
                          prefixIcon: Icons.mail_outline_rounded,
                          keyboardType: TextInputType.emailAddress,
                          textInputAction: TextInputAction.next,
                          autofillHints: const [AutofillHints.email],
                          autocorrect: false,
                          validator: _validateEmail,
                          onFieldSubmitted: (_) =>
                              _passwordFocus.requestFocus(),
                        ),
                        const SizedBox(height: AppSpacing.md),
                        LoginGlassField(
                          controller: _passwordController,
                          focusNode: _passwordFocus,
                          label: 'Password',
                          hint: 'Create a password',
                          prefixIcon: Icons.lock_outline_rounded,
                          obscureText: _obscurePassword,
                          textInputAction: TextInputAction.done,
                          autofillHints: const [AutofillHints.newPassword],
                          autocorrect: false,
                          validator: _validatePassword,
                          onFieldSubmitted: (_) => _onCreateAccount(),
                          suffixIcon: IconButton(
                            tooltip: _obscurePassword
                                ? 'Show password'
                                : 'Hide password',
                            onPressed: () {
                              setState(
                                () => _obscurePassword = !_obscurePassword,
                              );
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
                        const SizedBox(height: AppSpacing.xl),
                        _RegisterPillButton(
                          onPressed: _isSubmitting ? null : _onCreateAccount,
                          label: _isSubmitting
                              ? 'Creating account…'
                              : 'Create account',
                        ),
                        const SizedBox(height: AppSpacing.xl),
                        Center(
                          child: Text.rich(
                            TextSpan(
                              style: TextStyle(
                                fontSize: 14,
                                fontWeight: FontWeight.w400,
                                color: AppColors.splashTextMuted.withValues(
                                  alpha: 0.95,
                                ),
                              ),
                              children: [
                                const TextSpan(
                                  text: 'Already have an account? ',
                                ),
                                WidgetSpan(
                                  alignment: PlaceholderAlignment.baseline,
                                  baseline: TextBaseline.alphabetic,
                                    child: GestureDetector(
                                    onTap: _onLogIn,
                                    child: const Text(
                                      'Log in',
                                      style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.w700,
                                        color: Color(0xFFE1306C),
                                      ),
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            textAlign: TextAlign.center,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Stadium / pill gradient CTA matching the Register mockup.
class _RegisterPillButton extends StatelessWidget {
  const _RegisterPillButton({
    required this.onPressed,
    this.label = 'Create account',
  });

  final VoidCallback? onPressed;
  final String label;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      height: AppSpacing.buttonHeight,
      child: DecoratedBox(
        decoration: BoxDecoration(
          gradient: AppGradients.brandCta,
          borderRadius: AppRadius.circularButton,
          boxShadow: AppShadows.cta,
        ),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: onPressed,
            borderRadius: AppRadius.circularButton,
            child: Center(
              child: Text(
                label,
                style: const TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.2,
                  color: AppColors.splashTextPrimary,
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
