import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/route_paths.dart';
import '../../../../core/theme/app_animations.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../widgets/splash_bottom_panel.dart';
import '../widgets/splash_brand_block.dart';

/// App entry splash — brand + Get Started (unsigned users only).
///
/// Authenticated users never land here: [createAppRouter] starts at Home.
/// This screen stays visible until Get Started; it does not auto-navigate.
class SplashPage extends ConsumerStatefulWidget {
  const SplashPage({super.key});

  @override
  ConsumerState<SplashPage> createState() => _SplashPageState();
}

class _SplashPageState extends ConsumerState<SplashPage>
    with TickerProviderStateMixin {
  late final AnimationController _entranceController;
  late final AnimationController _pulseController;
  late final Animation<double> _fade;
  late final Animation<double> _scale;
  late final Animation<Offset> _slide;
  late final Animation<double> _pulse;

  var _isNavigating = false;

  @override
  void initState() {
    super.initState();
    _entranceController = AnimationController(
      vsync: this,
      duration: AppAnimations.splashEntrance,
    );
    _fade = CurvedAnimation(
      parent: _entranceController,
      curve: const Interval(0.0, 0.7, curve: AppAnimations.emphasized),
    );
    _scale = Tween<double>(begin: 0.92, end: 1).animate(
      CurvedAnimation(
        parent: _entranceController,
        curve: const Interval(0.0, 0.75, curve: AppAnimations.standard),
      ),
    );
    _slide = Tween<Offset>(
      begin: const Offset(0, 0.06),
      end: Offset.zero,
    ).animate(
      CurvedAnimation(
        parent: _entranceController,
        curve: const Interval(0.15, 0.85, curve: AppAnimations.standard),
      ),
    );

    _pulseController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 2400),
    )..repeat(reverse: true);
    _pulse = CurvedAnimation(
      parent: _pulseController,
      curve: Curves.easeInOut,
    );

    _entranceController.forward();
  }

  Future<void> _onGetStarted() async {
    if (_isNavigating || !mounted) return;
    setState(() => _isNavigating = true);

    // Pending Android share URL (if any) is kept for Login/Register → share route.
    if (!mounted) return;
    context.go(RoutePaths.login);
  }

  @override
  void dispose() {
    _entranceController.dispose();
    _pulseController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.splashBgDeep,
      body: Stack(
        fit: StackFit.expand,
        children: [
          const _SplashBackground(),
          SafeArea(
            bottom: false,
            child: Column(
              children: [
                Expanded(
                  child: FadeTransition(
                    opacity: _fade,
                    child: SlideTransition(
                      position: _slide,
                      child: ScaleTransition(
                        scale: _scale,
                        child: SplashBrandBlock(pulse: _pulse),
                      ),
                    ),
                  ),
                ),
                SplashBottomPanel(
                  isResolving: _isNavigating,
                  onGetStarted: _onGetStarted,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Layered background matching the mockup (linear wash + radial glows).
class _SplashBackground extends StatelessWidget {
  const _SplashBackground();

  @override
  Widget build(BuildContext context) {
    return const DecoratedBox(
      decoration: BoxDecoration(gradient: AppGradients.splashBackground),
      child: Stack(
        fit: StackFit.expand,
        children: [
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: Alignment(0, -0.85),
                radius: 1.05,
                colors: [
                  Color(0x662C1A2E),
                  Color(0x002C1A2E),
                ],
              ),
            ),
          ),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: Alignment(0.75, 0.95),
                radius: 0.95,
                colors: [
                  Color(0x550F1A2E),
                  Color(0x000F1A2E),
                ],
              ),
            ),
          ),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: Alignment(-0.8, 0.2),
                radius: 0.7,
                colors: [
                  Color(0x2212121B),
                  Color(0x0012121B),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
