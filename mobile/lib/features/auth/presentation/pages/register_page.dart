import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_constants.dart';
import '../../../../core/router/route_paths.dart';
import '../widgets/app_brand_mark.dart';
import '../widgets/auth_text_field.dart';

/// Register screen — account creation entry (SRS §7 / §16 / §22).
///
/// Client-side validation only. Registration API is a placeholder (no fake users).
class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});

  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  final _nameFocus = FocusNode();
  final _emailFocus = FocusNode();
  final _passwordFocus = FocusNode();
  final _confirmFocus = FocusNode();

  var _obscurePassword = true;
  var _obscureConfirmPassword = true;
  var _acceptedTerms = false;
  var _submitted = false;
  String? _termsError;

  late final TapGestureRecognizer _privacyTap;
  late final TapGestureRecognizer _termsTap;

  @override
  void initState() {
    super.initState();
    _privacyTap = TapGestureRecognizer()..onTap = _onPrivacyPlaceholder;
    _termsTap = TapGestureRecognizer()..onTap = _onTermsPlaceholder;
  }

  @override
  void dispose() {
    _privacyTap.dispose();
    _termsTap.dispose();
    _nameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    _nameFocus.dispose();
    _emailFocus.dispose();
    _passwordFocus.dispose();
    _confirmFocus.dispose();
    super.dispose();
  }

  void _onPrivacyPlaceholder() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Privacy policy link placeholder.')),
    );
  }

  void _onTermsPlaceholder() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Terms of use link placeholder.')),
    );
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
    return null;
  }

  String? _validateConfirmPassword(String? value) {
    final confirm = value ?? '';
    if (confirm.isEmpty) {
      return 'Confirm your password.';
    }
    if (confirm != _passwordController.text) {
      return 'Passwords do not match.';
    }
    return null;
  }

  Future<void> _onCreateAccount() async {
    FocusScope.of(context).unfocus();
    setState(() {
      _submitted = true;
      _termsError = _acceptedTerms
          ? null
          : 'Accept the Terms and Privacy policy to continue.';
    });

    final isValid = _formKey.currentState?.validate() ?? false;
    if (!isValid || !_acceptedTerms) return;

    // Placeholder only — do not call registration APIs or create a fake user.
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text(
          'Registration will use the backend auth API in a later sprint. No account was created.',
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    final width = MediaQuery.sizeOf(context).width;
    final horizontal = width >= 600 ? 32.0 : 24.0;

    return Scaffold(
      backgroundColor: scheme.surface,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        leading: IconButton(
          tooltip: 'Back to sign in',
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: () => context.go(RoutePaths.login),
        ),
      ),
      body: DecoratedBox(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [
              scheme.primary.withValues(
                alpha: scheme.brightness == Brightness.light ? 0.08 : 0.16,
              ),
              scheme.surface,
              scheme.surface,
            ],
            stops: const [0.0, 0.38, 1.0],
          ),
        ),
        child: SafeArea(
          top: false,
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
                    padding: EdgeInsets.fromLTRB(horizontal, 8, horizontal, 28),
                    children: [
                      const Align(
                        alignment: Alignment.centerLeft,
                        child: AppBrandMark(size: 64, iconSize: 32, borderRadius: 20),
                      ),
                      const SizedBox(height: 24),
                      Text(
                        'Create account',
                        style: textTheme.headlineSmall?.copyWith(
                          fontWeight: FontWeight.w700,
                          color: scheme.onSurface,
                          letterSpacing: -0.3,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Join ${AppConstants.appName} to save Instagram and Facebook reels to your library.',
                        style: textTheme.bodyLarge?.copyWith(
                          color: scheme.onSurface.withValues(alpha: 0.68),
                          height: 1.45,
                        ),
                      ),
                      const SizedBox(height: 28),
                      AuthTextField(
                        controller: _nameController,
                        focusNode: _nameFocus,
                        label: 'Display name',
                        prefixIcon: Icons.person_outline_rounded,
                        textInputAction: TextInputAction.next,
                        textCapitalization: TextCapitalization.words,
                        autofillHints: const [AutofillHints.name],
                        validator: _validateName,
                        onFieldSubmitted: (_) => _emailFocus.requestFocus(),
                      ),
                      const SizedBox(height: 14),
                      AuthTextField(
                        controller: _emailController,
                        focusNode: _emailFocus,
                        label: 'Email',
                        hint: 'you@example.com',
                        prefixIcon: Icons.mail_outline_rounded,
                        keyboardType: TextInputType.emailAddress,
                        textInputAction: TextInputAction.next,
                        autofillHints: const [AutofillHints.email],
                        autocorrect: false,
                        validator: _validateEmail,
                        onFieldSubmitted: (_) => _passwordFocus.requestFocus(),
                      ),
                      const SizedBox(height: 14),
                      AuthTextField(
                        controller: _passwordController,
                        focusNode: _passwordFocus,
                        label: 'Password',
                        prefixIcon: Icons.lock_outline_rounded,
                        obscureText: _obscurePassword,
                        textInputAction: TextInputAction.next,
                        autofillHints: const [AutofillHints.newPassword],
                        autocorrect: false,
                        validator: _validatePassword,
                        onFieldSubmitted: (_) => _confirmFocus.requestFocus(),
                        suffixIcon: IconButton(
                          tooltip: _obscurePassword ? 'Show password' : 'Hide password',
                          onPressed: () {
                            setState(() => _obscurePassword = !_obscurePassword);
                          },
                          icon: Icon(
                            _obscurePassword
                                ? Icons.visibility_outlined
                                : Icons.visibility_off_outlined,
                          ),
                        ),
                      ),
                      const SizedBox(height: 14),
                      AuthTextField(
                        controller: _confirmPasswordController,
                        focusNode: _confirmFocus,
                        label: 'Confirm password',
                        prefixIcon: Icons.lock_outline_rounded,
                        obscureText: _obscureConfirmPassword,
                        textInputAction: TextInputAction.done,
                        autofillHints: const [AutofillHints.newPassword],
                        autocorrect: false,
                        validator: _validateConfirmPassword,
                        onFieldSubmitted: (_) => _onCreateAccount(),
                        suffixIcon: IconButton(
                          tooltip: _obscureConfirmPassword
                              ? 'Show password'
                              : 'Hide password',
                          onPressed: () {
                            setState(
                              () => _obscureConfirmPassword = !_obscureConfirmPassword,
                            );
                          },
                          icon: Icon(
                            _obscureConfirmPassword
                                ? Icons.visibility_outlined
                                : Icons.visibility_off_outlined,
                          ),
                        ),
                      ),
                      const SizedBox(height: 16),
                      CheckboxListTile(
                        value: _acceptedTerms,
                        contentPadding: EdgeInsets.zero,
                        controlAffinity: ListTileControlAffinity.leading,
                        onChanged: (value) {
                          setState(() {
                            _acceptedTerms = value ?? false;
                            if (_acceptedTerms) {
                              _termsError = null;
                            }
                          });
                        },
                        title: Text.rich(
                          TextSpan(
                            style: textTheme.bodyMedium?.copyWith(
                              color: scheme.onSurface.withValues(alpha: 0.78),
                              height: 1.4,
                            ),
                            children: [
                              const TextSpan(text: 'I agree to the '),
                              TextSpan(
                                text: 'Terms',
                                style: TextStyle(
                                  color: scheme.primary,
                                  fontWeight: FontWeight.w600,
                                ),
                                recognizer: _termsTap,
                              ),
                              const TextSpan(text: ' and '),
                              TextSpan(
                                text: 'Privacy',
                                style: TextStyle(
                                  color: scheme.primary,
                                  fontWeight: FontWeight.w600,
                                ),
                                recognizer: _privacyTap,
                              ),
                              const TextSpan(
                                text:
                                    ' policy, and I will only save content I am authorized to access.',
                              ),
                            ],
                          ),
                        ),
                      ),
                      if (_termsError != null) ...[
                        Padding(
                          padding: const EdgeInsets.only(left: 12, top: 4),
                          child: Text(
                            _termsError!,
                            style: textTheme.bodySmall?.copyWith(
                              color: scheme.error,
                            ),
                          ),
                        ),
                      ],
                      const SizedBox(height: 20),
                      FilledButton(
                        onPressed: _onCreateAccount,
                        child: const Text('Create account'),
                      ),
                      const SizedBox(height: 12),
                      TextButton(
                        onPressed: () => context.go(RoutePaths.login),
                        child: const Text('Already have an account? Sign in'),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
