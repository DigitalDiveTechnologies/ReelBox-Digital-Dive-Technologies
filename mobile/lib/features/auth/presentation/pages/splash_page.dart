import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_constants.dart';
import '../../../../core/router/route_paths.dart';
import '../providers/auth_providers.dart';
import '../widgets/app_brand_mark.dart';

/// App entry splash — brand, then session gate (SRS entry before §6.2 Home).
///
/// Uses [CheckAuthStatusUseCase] as a placeholder until real session APIs exist.
/// Does not perform fake authentication.
class SplashPage extends ConsumerStatefulWidget {
  const SplashPage({super.key});

  @override
  ConsumerState<SplashPage> createState() => _SplashPageState();
}

class _SplashPageState extends ConsumerState<SplashPage>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  late final Animation<double> _fade;
  late final Animation<double> _scale;
  late final Animation<Offset> _slide;

  var _isResolvingSession = false;
  var _hasNavigated = false;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1100),
    );
    _fade = CurvedAnimation(
      parent: _controller,
      curve: const Interval(0.0, 0.7, curve: Curves.easeOut),
    );
    _scale = Tween<double>(begin: 0.92, end: 1).animate(
      CurvedAnimation(
        parent: _controller,
        curve: const Interval(0.0, 0.75, curve: Curves.easeOutCubic),
      ),
    );
    _slide = Tween<Offset>(
      begin: const Offset(0, 0.06),
      end: Offset.zero,
    ).animate(
      CurvedAnimation(
        parent: _controller,
        curve: const Interval(0.15, 0.85, curve: Curves.easeOutCubic),
      ),
    );

    _controller.forward();
    _controller.addStatusListener(_onAnimationStatus);
  }

  void _onAnimationStatus(AnimationStatus status) {
    if (status == AnimationStatus.completed) {
      _resolveEntry();
    }
  }

  Future<void> _resolveEntry() async {
    if (_hasNavigated || !mounted) return;

    setState(() => _isResolvingSession = true);

    // Brief pause so the brand is readable before routing.
    await Future<void>.delayed(const Duration(milliseconds: 450));
    if (!mounted || _hasNavigated) return;

    var isAuthenticated = false;
    try {
      isAuthenticated = await ref.read(checkAuthStatusUseCaseProvider).call();
    } catch (_) {
      // Placeholder auth must never block entry; treat failures as signed out.
      isAuthenticated = false;
    }

    if (!mounted || _hasNavigated) return;
    _hasNavigated = true;

    // Session present → product Home (§6.2). Otherwise → Login (account/session).
    context.go(isAuthenticated ? RoutePaths.home : RoutePaths.login);
  }

  @override
  void dispose() {
    _controller.removeStatusListener(_onAnimationStatus);
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;
    final size = MediaQuery.sizeOf(context);
    final isCompact = size.height < 680;

    return Scaffold(
      backgroundColor: scheme.surface,
      body: DecoratedBox(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [
              scheme.primary.withValues(alpha: scheme.brightness == Brightness.light ? 0.10 : 0.18),
              scheme.surface,
              scheme.surface,
            ],
            stops: const [0.0, 0.45, 1.0],
          ),
        ),
        child: SafeArea(
          child: Padding(
            padding: EdgeInsets.symmetric(
              horizontal: 28,
              vertical: isCompact ? 20 : 28,
            ),
            child: Column(
              children: [
                const Spacer(flex: 2),
                FadeTransition(
                  opacity: _fade,
                  child: SlideTransition(
                    position: _slide,
                    child: ScaleTransition(
                      scale: _scale,
                      child: Column(
                        children: [
                          AppBrandMark(
                            size: isCompact ? 76 : 88,
                            iconSize: isCompact ? 38 : 44,
                            borderRadius: isCompact ? 24 : 28,
                          ),
                          SizedBox(height: isCompact ? 22 : 28),
                          Text(
                            AppConstants.appName,
                            textAlign: TextAlign.center,
                            style: textTheme.headlineMedium?.copyWith(
                              fontWeight: FontWeight.w700,
                              color: scheme.onSurface,
                              letterSpacing: -0.4,
                            ),
                          ),
                          const SizedBox(height: 12),
                          ConstrainedBox(
                            constraints: const BoxConstraints(maxWidth: 320),
                            child: Text(
                              'Share a reel from Instagram or Facebook, or paste a link to save it to your library.',
                              textAlign: TextAlign.center,
                              style: textTheme.bodyLarge?.copyWith(
                                color: scheme.onSurface.withValues(alpha: 0.68),
                                height: 1.45,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
                const Spacer(flex: 3),
                AnimatedOpacity(
                  opacity: _isResolvingSession ? 1 : 0,
                  duration: const Duration(milliseconds: 220),
                  child: Column(
                    children: [
                      SizedBox(
                        width: 28,
                        height: 28,
                        child: CircularProgressIndicator(
                          strokeWidth: 2.5,
                          color: scheme.primary,
                        ),
                      ),
                      const SizedBox(height: 14),
                      Text(
                        'Checking session…',
                        style: textTheme.bodySmall?.copyWith(
                          color: scheme.onSurface.withValues(alpha: 0.55),
                        ),
                      ),
                    ],
                  ),
                ),
                SizedBox(height: isCompact ? 12 : 20),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
