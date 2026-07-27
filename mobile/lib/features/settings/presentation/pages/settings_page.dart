import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_exception.dart';
import '../../../../core/router/route_paths.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../auth/presentation/providers/auth_providers.dart';
import '../../../media/presentation/providers/media_providers.dart';

/// Settings screen (SRS §7) — account/session + logout.
///
/// Presentation is design-locked to the approved Settings mockup.
class SettingsPage extends ConsumerStatefulWidget {
  const SettingsPage({super.key});

  @override
  ConsumerState<SettingsPage> createState() => _SettingsPageState();
}

class _SettingsPageState extends ConsumerState<SettingsPage> {
  var _autoDownloadWifi = true;
  var _downloadNotifications = false;
  var _loggingOut = false;

  Future<void> _onLogout() async {
    if (_loggingOut) return;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) {
        return AlertDialog(
          backgroundColor: AppColors.splashSheet,
          title: const Text(
            'Sign out?',
            style: TextStyle(color: AppColors.splashTextPrimary),
          ),
          content: const Text(
            'You will need to log in again to access your library.',
            style: TextStyle(color: AppColors.splashTextMuted),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(context).pop(false),
              child: const Text('Cancel'),
            ),
            TextButton(
              onPressed: () => Navigator.of(context).pop(true),
              child: const Text('Sign out'),
            ),
          ],
        );
      },
    );
    if (confirmed != true || !mounted) return;

    setState(() => _loggingOut = true);
    try {
      await ref.read(authControllerProvider).logout();
      ref.invalidate(mediaListProvider);
      ref.invalidate(currentUserProvider);
      if (!mounted) return;
      context.go(RoutePaths.login);
    } on AppException catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(error.message)),
      );
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not sign out.')),
      );
    } finally {
      if (mounted) {
        setState(() => _loggingOut = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final horizontal = AppSpacing.horizontalInset(context);
    final mediaAsync = ref.watch(mediaListProvider);

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
                center: const Alignment(-0.9, -0.95),
                radius: 0.95,
                colors: [
                  AppColors.brandOrangeDeep.withValues(alpha: 0.28),
                  AppColors.brandOrangeDeep.withValues(alpha: 0),
                ],
              ),
            ),
          ),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: const Alignment(0.85, 0.95),
                radius: 0.95,
                colors: [
                  AppColors.splashBgNavy.withValues(alpha: 0.55),
                  AppColors.splashBgNavy.withValues(alpha: 0),
                ],
              ),
            ),
          ),
          SafeArea(
            child: Align(
              alignment: Alignment.topCenter,
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 560),
                child: ListView(
                  padding: EdgeInsets.fromLTRB(
                    horizontal,
                    AppSpacing.md,
                    horizontal,
                    AppSpacing.xxl,
                  ),
                  children: [
                    const Text(
                      'Settings',
                      style: TextStyle(
                        fontSize: 28,
                        fontWeight: FontWeight.w700,
                        letterSpacing: -0.4,
                        color: AppColors.splashTextPrimary,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.section),
                    const _SectionLabel('ACCOUNT'),
                    const SizedBox(height: AppSpacing.sm),
                    _SettingsCard(
                      child: ref.watch(currentUserProvider).when(
                        loading: () => const _AccountRow(
                          title: 'Loading…',
                          subtitle: 'Fetching session',
                          onTap: null,
                        ),
                        error: (error, stackTrace) => _AccountRow(
                          title: 'Session unavailable',
                          subtitle: 'Tap to sign in again',
                          onTap: () => context.go(RoutePaths.login),
                        ),
                        data: (user) {
                          final email = user?.email?.trim();
                          final hasSession =
                              email != null && email.isNotEmpty;
                          final title = hasSession
                              ? (email.contains('@')
                                  ? email.split('@').first
                                  : email)
                              : 'Not signed in';
                          final subtitle = hasSession
                              ? email
                              : 'Sign in to manage your library';

                          return _AccountRow(
                            title: title,
                            subtitle: subtitle,
                            enabled: !_loggingOut,
                            onTap: hasSession
                                ? _onLogout
                                : () => context.go(RoutePaths.login),
                          );
                        },
                      ),
                    ),
                    const SizedBox(height: AppSpacing.section),
                    const _SectionLabel('PREFERENCES'),
                    const SizedBox(height: AppSpacing.sm),
                    _SettingsCard(
                      child: Column(
                        children: [
                          _PreferenceToggleRow(
                            icon: Icons.wb_sunny_outlined,
                            label: 'Auto-download on Wi-Fi',
                            value: _autoDownloadWifi,
                            onChanged: (value) {
                              setState(() => _autoDownloadWifi = value);
                            },
                          ),
                          Divider(
                            height: 1,
                            thickness: 1,
                            color: AppColors.splashChipBorder.withValues(
                              alpha: 0.55,
                            ),
                          ),
                          _PreferenceToggleRow(
                            icon: Icons.notifications_none_rounded,
                            label: 'Download notifications',
                            value: _downloadNotifications,
                            onChanged: (value) {
                              setState(() => _downloadNotifications = value);
                            },
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: AppSpacing.section),
                    const _SectionLabel('STORAGE'),
                    const SizedBox(height: AppSpacing.sm),
                    _SettingsCard(
                      child: _StorageRow(
                        subtitle: mediaAsync.when(
                          data: (items) => items.isEmpty
                              ? 'Server library is empty'
                              : '${items.length} item${items.length == 1 ? '' : 's'} in library',
                          loading: () => 'Loading library…',
                          error: (error, stackTrace) =>
                              'Server-managed media library',
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(
      text,
      style: TextStyle(
        fontSize: 12,
        fontWeight: FontWeight.w600,
        letterSpacing: 1.2,
        color: AppColors.splashTextMuted.withValues(alpha: 0.9),
      ),
    );
  }
}

class _SettingsCard extends StatelessWidget {
  const _SettingsCard({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: AppRadius.circularCard,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: AppColors.splashSheet.withValues(alpha: 0.88),
          borderRadius: AppRadius.circularCard,
          border: Border.all(
            color: AppColors.splashChipBorder.withValues(alpha: 0.65),
          ),
        ),
        child: child,
      ),
    );
  }
}

class _LeadingIconBox extends StatelessWidget {
  const _LeadingIconBox({
    required this.icon,
    this.gradient,
  });

  final IconData icon;
  final Gradient? gradient;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 44,
      height: 44,
      decoration: BoxDecoration(
        gradient: gradient,
        color: gradient == null
            ? AppColors.splashChipFill.withValues(alpha: 0.85)
            : null,
        borderRadius: AppRadius.circularLg,
      ),
      child: Icon(
        icon,
        size: 22,
        color: AppColors.splashTextPrimary,
      ),
    );
  }
}

class _AccountRow extends StatelessWidget {
  const _AccountRow({
    required this.title,
    required this.subtitle,
    this.onTap,
    this.enabled = true,
  });

  final String title;
  final String subtitle;
  final VoidCallback? onTap;
  final bool enabled;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: enabled ? onTap : null,
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.cardPadding),
          child: Row(
            children: [
              const _LeadingIconBox(
                icon: Icons.person_rounded,
                gradient: AppGradients.brandCta,
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w600,
                        color: AppColors.splashTextPrimary,
                      ),
                    ),
                    const SizedBox(height: AppSpacing.xxs),
                    Text(
                      subtitle,
                      style: const TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w400,
                        color: AppColors.splashTextMuted,
                      ),
                    ),
                  ],
                ),
              ),
              Icon(
                Icons.logout_rounded,
                color: AppColors.splashTextMuted.withValues(alpha: 0.85),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _PreferenceToggleRow extends StatelessWidget {
  const _PreferenceToggleRow({
    required this.icon,
    required this.label,
    required this.value,
    required this.onChanged,
  });

  final IconData icon;
  final String label;
  final bool value;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: AppSpacing.md,
        vertical: AppSpacing.sm,
      ),
      child: Row(
        children: [
          _LeadingIconBox(icon: icon),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 15,
                fontWeight: FontWeight.w500,
                color: AppColors.splashTextPrimary,
              ),
            ),
          ),
          _GradientSwitch(value: value, onChanged: onChanged),
        ],
      ),
    );
  }
}

class _GradientSwitch extends StatelessWidget {
  const _GradientSwitch({
    required this.value,
    required this.onChanged,
  });

  final bool value;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => onChanged(!value),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 180),
        width: 52,
        height: 32,
        padding: const EdgeInsets.all(3),
        decoration: BoxDecoration(
          borderRadius: AppRadius.circularPill,
          gradient: value ? AppGradients.brandCta : null,
          color: value
              ? null
              : AppColors.splashChipBorder.withValues(alpha: 0.85),
        ),
        child: AnimatedAlign(
          duration: const Duration(milliseconds: 180),
          curve: Curves.easeOut,
          alignment: value ? Alignment.centerRight : Alignment.centerLeft,
          child: Container(
            width: 26,
            height: 26,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: value
                  ? AppColors.splashTextPrimary
                  : AppColors.splashTextMuted,
            ),
          ),
        ),
      ),
    );
  }
}

class _StorageRow extends StatelessWidget {
  const _StorageRow({required this.subtitle});

  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(AppSpacing.cardPadding),
      child: Row(
        children: [
          const _LeadingIconBox(icon: Icons.inventory_2_outlined),
          const SizedBox(width: AppSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Cache & storage',
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w600,
                    color: AppColors.splashTextPrimary,
                  ),
                ),
                const SizedBox(height: AppSpacing.xxs),
                Text(
                  subtitle,
                  style: const TextStyle(
                    fontSize: 13,
                    fontWeight: FontWeight.w400,
                    color: AppColors.splashTextMuted,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
