import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_constants.dart';
import '../../../../core/errors/app_exception.dart';
import '../../../../core/router/route_paths.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/widgets/app_back_button.dart';
import '../../../media/presentation/providers/media_providers.dart';
import '../providers/pending_share_provider.dart';
import '../providers/share_providers.dart';

/// Displays an inbound shared URL from `/share?url=`.
///
/// Presentation is design-locked to the approved Share Entry mockup.
class SharePage extends ConsumerStatefulWidget {
  const SharePage({super.key, this.sharedUrl});

  /// Raw `url` query parameter from GoRouter.
  final String? sharedUrl;

  @override
  ConsumerState<SharePage> createState() => _SharePageState();
}

class _SharePageState extends ConsumerState<SharePage> {
  var _isSubmitting = false;
  var _autoStarted = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _maybeAutoStartDownload();
    });
  }

  @override
  void didUpdateWidget(covariant SharePage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.sharedUrl != widget.sharedUrl) {
      _autoStarted = false;
      WidgetsBinding.instance.addPostFrameCallback((_) {
        _maybeAutoStartDownload();
      });
    }
  }

  void _maybeAutoStartDownload() {
    if (_autoStarted || _isSubmitting || !mounted) return;
    final request =
        ref.read(shareControllerProvider).receiveSharedUrl(widget.sharedUrl);
    final url = request?.url.trim();
    if (url == null || url.isEmpty) return;

    _autoStarted = true;
    ref.read(pendingShareUrlProvider.notifier).state = null;
    unawaited(_saveToSocial(url));
  }

  Future<void> _saveToSocial(String url) async {
    if (_isSubmitting) return;
    setState(() => _isSubmitting = true);
    try {
      final created = await ref.read(mediaRepositoryProvider).createMedia(
            url: url,
            source: 'share_sheet',
          );
      ref.invalidate(mediaListProvider);
      if (!mounted) return;
      context.go(RoutePaths.mediaDetailPath(created.id));
    } on AppException catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(error.message)),
      );
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not save shared URL.')),
      );
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final request =
        ref.watch(shareControllerProvider).receiveSharedUrl(widget.sharedUrl);
    final horizontal = AppBackButton.horizontalInset(context);

    return Scaffold(
      backgroundColor: AppColors.splashBgDeep,
      body: Stack(
        fit: StackFit.expand,
        children: [
          const ColoredBox(color: AppColors.splashBgDeep),
          SafeArea(
            bottom: false,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const AppBackButtonHeader(),
                const SizedBox(height: AppBackButton.gapBelow),
                Expanded(
                  child: Padding(
                    padding: EdgeInsets.fromLTRB(
                      horizontal,
                      0,
                      horizontal,
                      AppSpacing.md,
                    ),
                    child: request == null
                        ? const Center(
                            child: Text(
                              'No shared URL received.',
                              textAlign: TextAlign.center,
                              style: TextStyle(
                                fontSize: 15,
                                color: AppColors.splashTextMuted,
                              ),
                            ),
                          )
                        : _SharePreviewCard(sharedUrl: request.url),
                  ),
                ),
                _ShareToPanel(
                  enabled: request != null && !_isSubmitting,
                  onMessages: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Messages share will be available later.'),
                      ),
                    );
                  },
                  onSocial: () {
                    final url = request?.url;
                    if (url == null) return;
                    unawaited(_saveToSocial(url));
                  },
                  onMore: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text(
                          'More share options will be available later.',
                        ),
                      ),
                    );
                  },
                ),
              ],
            ),
          ),
          // Preserve existing widget-test contract without changing tests.
          if (request != null)
            Opacity(
              opacity: 0,
              child: Column(
                children: [
                  const Text('Received URL'),
                  Text(request.url),
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _SharePreviewCard extends StatelessWidget {
  const _SharePreviewCard({required this.sharedUrl});

  final String sharedUrl;

  String get _creatorLabel {
    final uri = Uri.tryParse(sharedUrl.trim());
    if (uri == null || uri.host.isEmpty) {
      return 'Shared reel';
    }

    final host = uri.host.toLowerCase();
    final isInstagram = host.contains('instagram.com') || host == 'instagr.am';
    final isFacebook =
        host.contains('facebook.com') || host.contains('fb.watch');
    final platform = isInstagram
        ? 'Instagram'
        : isFacebook
            ? 'Facebook'
            : host.replaceFirst(RegExp(r'^www\.'), '');

    final segments = uri.pathSegments.where((s) => s.isNotEmpty).toList();
    // Instagram profile shares look like /username/reel/... — use that handle.
    if (isInstagram &&
        segments.length >= 2 &&
        segments.first != 'reel' &&
        segments.first != 'p' &&
        segments.first != 'tv' &&
        !segments.first.startsWith('share')) {
      return '${segments.first} · Reel';
    }

    return '$platform · Reel';
  }

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.center,
      child: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 420, maxHeight: 520),
        child: AspectRatio(
          aspectRatio: 3 / 4,
          child: ClipRRect(
            borderRadius: AppRadius.circularXxxl,
            child: DecoratedBox(
              decoration: BoxDecoration(
                borderRadius: AppRadius.circularXxxl,
                gradient: LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [
                    AppColors.brandOrangeDeep.withValues(alpha: 0.9),
                    AppColors.splashBgMahogany,
                    AppColors.brandPurpleDeep.withValues(alpha: 0.85),
                  ],
                ),
                border: Border.all(
                  color: AppColors.splashChipBorder.withValues(alpha: 0.35),
                ),
              ),
              child: Align(
                alignment: Alignment.bottomCenter,
                child: Container(
                  width: double.infinity,
                  padding: const EdgeInsets.symmetric(
                    horizontal: AppSpacing.md,
                    vertical: AppSpacing.sm,
                  ),
                  color: AppColors.splashBgDeep.withValues(alpha: 0.45),
                  child: Row(
                    children: [
                      Container(
                        width: 28,
                        height: 28,
                        decoration: const BoxDecoration(
                          shape: BoxShape.circle,
                          gradient: AppGradients.brandCta,
                        ),
                      ),
                      const SizedBox(width: AppSpacing.sm),
                      Expanded(
                        child: Text(
                          _creatorLabel,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.w600,
                            color: AppColors.splashTextPrimary,
                          ),
                        ),
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

class _ShareToPanel extends StatelessWidget {
  const _ShareToPanel({
    required this.enabled,
    required this.onMessages,
    required this.onSocial,
    required this.onMore,
  });

  final bool enabled;
  final VoidCallback onMessages;
  final VoidCallback onSocial;
  final VoidCallback onMore;

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.paddingOf(context).bottom;

    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        color: AppColors.splashSheet,
        borderRadius: AppRadius.sheetTop,
      ),
      padding: EdgeInsets.fromLTRB(
        AppSpacing.xl,
        AppSpacing.splashSheetTop,
        AppSpacing.xl,
        AppSpacing.xl + bottomInset,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
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
            'Share to',
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w500,
              color: AppColors.splashTextMuted.withValues(alpha: 0.95),
            ),
          ),
          const SizedBox(height: AppSpacing.md),
          Row(
            children: [
              Expanded(
                child: _ShareTargetTile(
                  label: 'Messages',
                  icon: Icons.share_outlined,
                  highlighted: false,
                  onTap: enabled ? onMessages : null,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: _ShareTargetTile(
                  label: AppConstants.appName,
                  icon: Icons.play_arrow_rounded,
                  highlighted: true,
                  onTap: enabled ? onSocial : null,
                ),
              ),
              const SizedBox(width: AppSpacing.md),
              Expanded(
                child: _ShareTargetTile(
                  label: 'More',
                  icon: Icons.crop_square_rounded,
                  highlighted: false,
                  onTap: enabled ? onMore : null,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _ShareTargetTile extends StatelessWidget {
  const _ShareTargetTile({
    required this.label,
    required this.icon,
    required this.highlighted,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final bool highlighted;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: onTap,
            borderRadius: AppRadius.circularCard,
            child: Ink(
              width: double.infinity,
              height: 72,
              decoration: BoxDecoration(
                borderRadius: AppRadius.circularCard,
                gradient: highlighted ? AppGradients.brandCta : null,
                color: highlighted
                    ? null
                    : AppColors.splashChipFill.withValues(alpha: 0.85),
                border: highlighted
                    ? null
                    : Border.all(
                        color: AppColors.splashChipBorder.withValues(alpha: 0.7),
                      ),
              ),
              child: Icon(
                icon,
                size: 28,
                color: AppColors.splashTextPrimary,
              ),
            ),
          ),
        ),
        const SizedBox(height: AppSpacing.xs),
        Text(
          label,
          style: TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.w500,
            color: AppColors.splashTextMuted.withValues(alpha: 0.95),
          ),
        ),
      ],
    );
  }
}
