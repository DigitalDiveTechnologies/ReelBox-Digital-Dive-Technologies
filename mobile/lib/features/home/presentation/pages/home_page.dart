import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_exception.dart';
import '../../../../core/router/route_paths.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_platform.dart';
import '../../../../shared/models/media_status.dart';
import '../../../media/presentation/providers/media_providers.dart';
import '../../../share/data/share_url_extractor.dart';
import '../widgets/home_header_bar.dart';
import '../widgets/home_paste_link_card.dart';
import '../widgets/home_platform_card.dart';
import '../widgets/home_recent_reel_card.dart';

/// Home screen — download dashboard (SRS §6.2 / §7).
///
/// Presentation is design-locked to the approved Home mockup.
class HomePage extends ConsumerStatefulWidget {
  const HomePage({super.key});

  @override
  ConsumerState<HomePage> createState() => _HomePageState();
}

class _HomePageState extends ConsumerState<HomePage> {
  var _isSubmitting = false;

  Future<void> _onPasteLink() async {
    final data = await Clipboard.getData(Clipboard.kTextPlain);
    final text = data?.text?.trim();
    if (!mounted) return;

    if (text == null || text.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Clipboard is empty.')),
      );
      return;
    }

    final url = ShareUrlExtractor.extract(text);
    if (url == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('No Instagram or Facebook URL found.')),
      );
      return;
    }

    await _submitUrl(url, source: 'clipboard');
  }

  Future<void> _submitUrl(String url, {required String source}) async {
    if (_isSubmitting) return;
    setState(() => _isSubmitting = true);

    try {
      final created = await ref.read(mediaRepositoryProvider).createMedia(
            url: url,
            source: source,
          );
      ref.invalidate(mediaListProvider);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Saving… status: ${created.status.name}')),
      );
      context.push(RoutePaths.mediaDetailPath(created.id));
    } on AppException catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(error.message)),
      );
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not save this link.')),
      );
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  void _onNotificationPlaceholder() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Notifications will be available later.')),
    );
  }

  void _openMedia(String id) {
    context.push(RoutePaths.mediaDetailPath(id));
  }

  void _onSeeAll() {
    context.go(RoutePaths.library);
  }

  void _onPlatformTap(MediaPlatform platform) {
    context.go(RoutePaths.library);
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
                child: RefreshIndicator(
                  color: AppColors.splashTextPrimary,
                  backgroundColor: AppColors.splashBgNavy,
                  onRefresh: () async {
                    ref.invalidate(mediaListProvider);
                    await ref.read(mediaListProvider.future);
                  },
                  child: ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: EdgeInsets.fromLTRB(
                      horizontal,
                      AppSpacing.md,
                      horizontal,
                      AppSpacing.xxl,
                    ),
                    children: [
                      HomeHeaderBar(onNotificationTap: _onNotificationPlaceholder),
                      const SizedBox(height: AppSpacing.section),
                      const Text(
                        'Your saved reels',
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
                        'Share from Instagram or Facebook anytime.',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w400,
                          height: 1.4,
                          color: AppColors.splashTextMuted.withValues(alpha: 0.95),
                        ),
                      ),
                      const SizedBox(height: AppSpacing.xl),
                      HomePasteLinkCard(
                        onTap: _isSubmitting ? () {} : _onPasteLink,
                      ),
                      const SizedBox(height: AppSpacing.md),
                      mediaAsync.when(
                        loading: () => const Padding(
                          padding: EdgeInsets.symmetric(vertical: AppSpacing.xl),
                          child: Center(
                            child: CircularProgressIndicator(
                              color: AppColors.splashTextPrimary,
                            ),
                          ),
                        ),
                        error: (error, _) => _HomeBody(
                          items: const [],
                          igCount: 0,
                          fbCount: 0,
                          errorMessage: error is AppException
                              ? error.message
                              : 'Could not load your library.',
                          onPlatformTap: _onPlatformTap,
                          onSeeAll: _onSeeAll,
                          onOpenMedia: _openMedia,
                        ),
                        data: (items) {
                          final igCount = items
                              .where((e) => e.platform == MediaPlatform.instagram)
                              .length;
                          final fbCount = items
                              .where((e) => e.platform == MediaPlatform.facebook)
                              .length;
                          final recent = items.take(8).toList(growable: false);
                          return _HomeBody(
                            items: recent,
                            igCount: igCount,
                            fbCount: fbCount,
                            onPlatformTap: _onPlatformTap,
                            onSeeAll: _onSeeAll,
                            onOpenMedia: _openMedia,
                          );
                        },
                      ),
                    ],
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

class _HomeBody extends StatelessWidget {
  const _HomeBody({
    required this.items,
    required this.igCount,
    required this.fbCount,
    required this.onPlatformTap,
    required this.onSeeAll,
    required this.onOpenMedia,
    this.errorMessage,
  });

  final List<MediaItemPreview> items;
  final int igCount;
  final int fbCount;
  final ValueChanged<MediaPlatform> onPlatformTap;
  final VoidCallback onSeeAll;
  final ValueChanged<String> onOpenMedia;
  final String? errorMessage;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (errorMessage != null) ...[
          Text(
            errorMessage!,
            style: const TextStyle(
              color: AppColors.splashTextMuted,
              fontSize: 13,
            ),
          ),
          const SizedBox(height: AppSpacing.md),
        ],
        Row(
          children: [
            Expanded(
              child: HomePlatformCard(
                platform: MediaPlatform.instagram,
                savedLabel: '$igCount saved',
                onTap: () => onPlatformTap(MediaPlatform.instagram),
              ),
            ),
            const SizedBox(width: AppSpacing.md),
            Expanded(
              child: HomePlatformCard(
                platform: MediaPlatform.facebook,
                savedLabel: '$fbCount saved',
                onTap: () => onPlatformTap(MediaPlatform.facebook),
              ),
            ),
          ],
        ),
        const SizedBox(height: AppSpacing.section),
        Row(
          children: [
            const Text(
              'Recent',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w700,
                color: AppColors.splashTextPrimary,
              ),
            ),
            const Spacer(),
            TextButton(
              onPressed: onSeeAll,
              style: TextButton.styleFrom(
                foregroundColor: AppColors.splashTextMuted,
                padding: const EdgeInsets.symmetric(
                  horizontal: AppSpacing.xs,
                ),
                minimumSize: Size.zero,
                tapTargetSize: MaterialTapTargetSize.shrinkWrap,
              ),
              child: const Text(
                'See all',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w500,
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: AppSpacing.md),
        if (items.isEmpty)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: AppSpacing.lg),
            child: Text(
              'No saved reels yet. Paste a link to get started.',
              style: TextStyle(
                fontSize: 14,
                color: AppColors.splashTextMuted.withValues(alpha: 0.95),
              ),
            ),
          )
        else
          SizedBox(
            height: MediaQuery.sizeOf(context).width < 360 ? 160 : 176,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: items.length,
              separatorBuilder: (context, index) =>
                  const SizedBox(width: AppSpacing.md),
              itemBuilder: (context, index) {
                final item = items[index];
                return HomeRecentReelCard(
                  item: item,
                  onTap: () => onOpenMedia(item.id),
                );
              },
            ),
          ),
        if (items.any((e) =>
            e.status == MediaStatus.queued ||
            e.status == MediaStatus.downloading ||
            e.status == MediaStatus.processing)) ...[
          const SizedBox(height: AppSpacing.md),
          Text(
            'Downloads update automatically — pull to refresh.',
            style: TextStyle(
              fontSize: 12,
              color: AppColors.splashTextMuted.withValues(alpha: 0.9),
            ),
          ),
        ],
      ],
    );
  }
}
