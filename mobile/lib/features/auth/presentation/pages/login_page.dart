import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_exception.dart';
import '../../../../core/router/route_paths.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../share/presentation/providers/pending_share_provider.dart';
import '../providers/auth_providers.dart';
import '../widgets/login_brand_mark.dart';
import '../widgets/login_glass_field.dart';

/// Login screen — account/session entry (SRS §7 / §22).
///
/// Presentation is design-locked to the approved Login mockup.
class LoginPage extends ConsumerStatefulWidget {
  const LoginPage({super.key});

  @override
  ConsumerState<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends ConsumerState<LoginPage> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _emailFocus = FocusNode();
  final _passwordFocus = FocusNode();

  var _obscurePassword = true;
  var _submitted = false;
  var _isSubmitting = false;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    _emailFocus.dispose();
    _passwordFocus.dispose();
    super.dispose();
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
      return 'Enter your password.';
    }
    if (password.length < 8) {
      return 'Password must be at least 8 characters.';
    }
    return null;
  }

  void _onForgotPassword() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Password reset will be available when auth APIs are connected.'),
      ),
    );
  }

  Future<void> _onSignIn() async {
    FocusScope.of(context).unfocus();
    setState(() => _submitted = true);

    final isValid = _formKey.currentState?.validate() ?? false;
    if (!isValid) return;

    setState(() => _isSubmitting = true);
    try {
      await ref.read(authNotifierProvider.notifier).login(
            email: _emailController.text.trim(),
            password: _passwordController.text,
          );
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
        const SnackBar(content: Text('Sign-in failed. Please try again.')),
      );
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  void _onSignUp() {
    context.go(RoutePaths.register);
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
                          'Welcome back',
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
                          'Log in to access your saved reels',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w400,
                            letterSpacing: 0.1,
                            height: 1.4,
                            color: AppColors.splashTextMuted.withValues(alpha: 0.95),
                          ),
                        ),
                        const SizedBox(height: AppSpacing.section),
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
                          onFieldSubmitted: (_) => _passwordFocus.requestFocus(),
                        ),
                        const SizedBox(height: AppSpacing.md),
                        LoginGlassField(
                          controller: _passwordController,
                          focusNode: _passwordFocus,
                          label: 'Password',
                          hint: '••••••••',
                          prefixIcon: Icons.lock_outline_rounded,
                          obscureText: _obscurePassword,
                          textInputAction: TextInputAction.done,
                          autofillHints: const [AutofillHints.password],
                          autocorrect: false,
                          validator: _validatePassword,
                          onFieldSubmitted: (_) => _onSignIn(),
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
                        const SizedBox(height: AppSpacing.xs),
                        Align(
                          alignment: Alignment.centerRight,
                          child: TextButton(
                            onPressed: _onForgotPassword,
                            style: TextButton.styleFrom(
                              foregroundColor: AppColors.splashTextMuted,
                              padding: const EdgeInsets.symmetric(
                                horizontal: AppSpacing.xs,
                                vertical: AppSpacing.xxs,
                              ),
                              minimumSize: Size.zero,
                              tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                            ),
                            child: const Text(
                              'Forgot password?',
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w400,
                              ),
                            ),
                          ),
                        ),
                        const SizedBox(height: AppSpacing.lg),
                        LoginGradientButton(
                          label: _isSubmitting ? 'Signing in…' : 'Log in',
                          onPressed: _isSubmitting ? null : _onSignIn,
                        ),
                        const SizedBox(height: AppSpacing.xl),
                        Row(
                          children: [
                            Expanded(
                              child: Divider(
                                color: AppColors.splashChipBorder.withValues(
                                  alpha: 0.9,
                                ),
                                thickness: 1,
                              ),
                            ),
                            Padding(
                              padding: const EdgeInsets.symmetric(
                                horizontal: AppSpacing.sm,
                              ),
                              child: Text(
                                'OR',
                                style: TextStyle(
                                  fontSize: 11,
                                  fontWeight: FontWeight.w600,
                                  letterSpacing: 0.8,
                                  color: AppColors.splashTextMuted.withValues(
                                    alpha: 0.85,
                                  ),
                                ),
                              ),
                            ),
                            Expanded(
                              child: Divider(
                                color: AppColors.splashChipBorder.withValues(
                                  alpha: 0.9,
                                ),
                                thickness: 1,
                              ),
                            ),
                          ],
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
                                const TextSpan(text: "Don't have an account? "),
                                WidgetSpan(
                                  alignment: PlaceholderAlignment.baseline,
                                  baseline: TextBaseline.alphabetic,
                                  child: GestureDetector(
                                    onTap: _onSignUp,
                                    child: const Text(
                                      'Sign up',
                                      style: TextStyle(
                                        fontSize: 14,
                                        fontWeight: FontWeight.w700,
                                        // Instagram pink (not brand purple).
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
