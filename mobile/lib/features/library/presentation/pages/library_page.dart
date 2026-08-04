import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_exception.dart';
import '../../../../core/router/route_paths.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_platform.dart';
import '../../../../shared/models/reel_category.dart';
import '../../../../shared/widgets/app_empty_state.dart';
import '../../../media/presentation/providers/media_providers.dart';
import '../widgets/library_filter_chips.dart';
import '../widgets/library_media_card.dart';

/// Library / Downloads screen (SRS §7 / FR-013) — real API data only.
///
/// Presentation is design-locked to the approved Library mockup.
class LibraryPage extends ConsumerStatefulWidget {
  const LibraryPage({super.key});

  @override
  ConsumerState<LibraryPage> createState() => _LibraryPageState();
}

class _LibraryPageState extends ConsumerState<LibraryPage>
    with WidgetsBindingObserver {
  MediaPlatform? _platformFilter;
  String? _categoryFilter;
  Timer? _pollTimer;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _pollTimer = Timer.periodic(const Duration(seconds: 3), (_) {
      final async = ref.read(mediaListProvider);
      final items = async.asData?.value;
      if (items == null) return;
      if (items.any((e) => e.isActive)) {
        ref.invalidate(mediaListProvider);
      }
    });
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      ref.invalidate(mediaListProvider);
    }
  }

  List<MediaItemPreview> _filtered(List<MediaItemPreview> items) {
    var filtered = List<MediaItemPreview>.from(items);
    if (_platformFilter != null) {
      filtered = filtered.where((m) => m.platform == _platformFilter).toList();
    }
    if (_categoryFilter != null) {
      filtered = filtered
          .where((m) =>
              (m.category ?? ReelCategory.general) == _categoryFilter)
          .toList();
    }
    filtered.sort((a, b) => b.createdAt.compareTo(a.createdAt));
    return filtered;
  }

  Future<void> _refresh() async {
    ref.invalidate(mediaListProvider);
    await ref.read(mediaListProvider.future);
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
                center: const Alignment(-0.85, -0.95),
                radius: 0.95,
                colors: [
                  AppColors.splashBgMahogany.withValues(alpha: 0.5),
                  AppColors.splashBgMahogany.withValues(alpha: 0),
                ],
              ),
            ),
          ),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: const Alignment(0.9, -0.7),
                radius: 0.9,
                colors: [
                  AppColors.brandPurple.withValues(alpha: 0.18),
                  AppColors.brandPurple.withValues(alpha: 0),
                ],
              ),
            ),
          ),
          DecoratedBox(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: const Alignment(0.6, 0.95),
                radius: 0.85,
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
                constraints: const BoxConstraints(maxWidth: 720),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Padding(
                      padding: EdgeInsets.fromLTRB(
                        horizontal,
                        AppSpacing.md,
                        horizontal,
                        0,
                      ),
                      child: const Text(
                        'Library',
                        style: TextStyle(
                          fontSize: 28,
                          fontWeight: FontWeight.w700,
                          letterSpacing: -0.4,
                          color: AppColors.splashTextPrimary,
                        ),
                      ),
                    ),
                    const SizedBox(height: AppSpacing.md),
                    Padding(
                      padding: EdgeInsets.symmetric(horizontal: horizontal),
                      child: LibraryFilterChips(
                        selected: _platformFilter,
                        onSelected: (platform) {
                          setState(() => _platformFilter = platform);
                        },
                      ),
                    ),
                    const SizedBox(height: AppSpacing.sm),
                    Padding(
                      padding: EdgeInsets.symmetric(horizontal: horizontal),
                      child: LibraryCategoryFilterChips(
                        selected: _categoryFilter,
                        onSelected: (category) {
                          setState(() => _categoryFilter = category);
                        },
                      ),
                    ),
                    const SizedBox(height: AppSpacing.lg),
                    Expanded(
                      child: mediaAsync.when(
                        loading: () => const Center(
                          child: CircularProgressIndicator(
                            color: AppColors.splashTextPrimary,
                          ),
                        ),
                        error: (error, _) => _brandThemed(
                          context,
                          child: AppEmptyState(
                            icon: Icons.cloud_off_outlined,
                            title: 'Could not load library',
                            message: error is AppException
                                ? error.message
                                : 'Check your connection and try again.',
                          ),
                        ),
                        data: (allItems) {
                          final items = _filtered(allItems);
                          if (items.isEmpty) {
                            return RefreshIndicator(
                              color: AppColors.splashTextPrimary,
                              backgroundColor: AppColors.splashBgNavy,
                              onRefresh: _refresh,
                              child: ListView(
                                physics: const AlwaysScrollableScrollPhysics(),
                                children: [
                                  const SizedBox(height: 120),
                                  _brandThemed(
                                    context,
                                    child: const AppEmptyState(
                                      icon: Icons.video_library_outlined,
                                      title: 'No media found',
                                      message:
                                          'Saved reels appear here. Adjust filters, or save a new URL from Home.',
                                    ),
                                  ),
                                ],
                              ),
                            );
                          }

                          return RefreshIndicator(
                            color: AppColors.splashTextPrimary,
                            backgroundColor: AppColors.splashBgNavy,
                            onRefresh: _refresh,
                            child: GridView.builder(
                              physics: const AlwaysScrollableScrollPhysics(),
                              padding: EdgeInsets.fromLTRB(
                                horizontal,
                                0,
                                horizontal,
                                AppSpacing.xxl,
                              ),
                              gridDelegate:
                                  const SliverGridDelegateWithFixedCrossAxisCount(
                                crossAxisCount: 2,
                                mainAxisSpacing: AppSpacing.md,
                                crossAxisSpacing: AppSpacing.md,
                                childAspectRatio: 0.72,
                              ),
                              itemCount: items.length,
                              itemBuilder: (context, index) {
                                final item = items[index];
                                return LibraryMediaCard(
                                  key: ValueKey<String>('library-card-${item.id}'),
                                  item: item,
                                  onTap: () => context.push(
                                    RoutePaths.mediaDetailPath(item.id),
                                  ),
                                );
                              },
                            ),
                          );
                        },
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

  /// Splash/brand colors for empty-state icon (no Material teal seed).
  static Widget _brandThemed(BuildContext context, {required Widget child}) {
    final base = Theme.of(context);
    return Theme(
      data: base.copyWith(
        colorScheme: base.colorScheme.copyWith(
          primary: AppColors.brandPurple,
          primaryContainer: AppColors.brandPurpleDeep,
          onSurface: AppColors.splashTextPrimary,
        ),
        textTheme: base.textTheme.copyWith(
          titleMedium: base.textTheme.titleMedium?.copyWith(
            color: AppColors.splashTextPrimary,
            fontWeight: FontWeight.w700,
          ),
          bodyMedium: base.textTheme.bodyMedium?.copyWith(
            color: AppColors.splashTextMuted.withValues(alpha: 0.95),
          ),
        ),
      ),
      child: child,
    );
  }
}
