import 'dart:async';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:path/path.dart' as p;

import '../../../../core/errors/app_exception.dart';
import '../../../../core/router/route_paths.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_shadows.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_platform.dart';
import '../../../../shared/models/media_status.dart';
import '../../../../shared/widgets/app_back_button.dart';
import '../../../../shared/widgets/instagram_icon.dart';
import '../../../media/presentation/providers/media_providers.dart';

/// Media detail screen (SRS §7 / FR-014–016).
///
/// Presentation is design-locked to the approved Media Detail mockup.
class MediaDetailPage extends ConsumerStatefulWidget {
  const MediaDetailPage({super.key, required this.mediaId});

  final String mediaId;

  @override
  ConsumerState<MediaDetailPage> createState() => _MediaDetailPageState();
}

class _MediaDetailPageState extends ConsumerState<MediaDetailPage> {
  Timer? _pollTimer;

  @override
  void initState() {
    super.initState();
    _pollTimer = Timer.periodic(const Duration(seconds: 3), (_) {
      final async = ref.read(mediaDetailProvider(widget.mediaId));
      final item = async.asData?.value;
      if (item == null) return;
      if (item.status == MediaStatus.completed ||
          item.status == MediaStatus.failed) {
        return;
      }
      ref.invalidate(mediaDetailProvider(widget.mediaId));
    });
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    super.dispose();
  }

  void _onPlay(MediaItemPreview item) {
    if (item.status != MediaStatus.completed) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Media is ${item.status.name}.')),
      );
      return;
    }
    context.push(RoutePaths.mediaPlayerPath(item.id));
  }

  Future<void> _onDelete() async {
    try {
      await ref.read(mediaRepositoryProvider).deleteMedia(widget.mediaId);
      ref.invalidate(mediaListProvider);
      if (!mounted) return;
      context.pop();
    } on AppException catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(error.message)),
      );
    }
  }

  Future<void> _onRetry() async {
    try {
      await ref.read(mediaRepositoryProvider).retryMedia(widget.mediaId);
      ref.invalidate(mediaDetailProvider(widget.mediaId));
      ref.invalidate(mediaListProvider);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Retry queued.')),
      );
    } on AppException catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(error.message)),
      );
    }
  }

  Future<void> _onShare(MediaItemPreview item) async {
    if (item.status != MediaStatus.completed) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Media is not ready to share yet.')),
      );
      return;
    }

    try {
      final playback =
          await ref.read(mediaRepositoryProvider).getPlayback(item.id);
      final url = playback.playbackUrl?.trim();
      if (url == null || url.isEmpty) {
        throw const AppException(message: 'Playback URL unavailable.');
      }

      final response = await http.get(Uri.parse(url));
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw AppException(
          message: 'Could not download media to share (${response.statusCode}).',
        );
      }

      final mime = (playback.mimeType ?? item.mimeType ?? 'video/mp4')
          .split(';')
          .first
          .trim();
      final extension = switch (mime) {
        'video/quicktime' => '.mov',
        'image/jpeg' => '.jpg',
        'image/png' => '.png',
        'image/webp' => '.webp',
        _ => '.mp4',
      };

      final dir = Directory.systemTemp;
      final filePath = p.join(dir.path, 'srs-share-${item.id}$extension');
      final file = File(filePath);
      await file.writeAsBytes(response.bodyBytes, flush: true);

      const channel = MethodChannel('com.example.mobile/share_intent');
      await channel.invokeMethod<bool>('shareFile', {
        'path': filePath,
        'mimeType': mime,
        'text': item.displayTitle,
      });
    } on AppException catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(error.message)),
      );
    } catch (_) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Could not share downloaded media.')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final async = ref.watch(mediaDetailProvider(widget.mediaId));
    final horizontal = AppBackButton.horizontalInset(context);

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
                center: const Alignment(0.2, -0.6),
                radius: 1.1,
                colors: [
                  AppColors.splashBgNavy.withValues(alpha: 0.55),
                  AppColors.splashBgNavy.withValues(alpha: 0),
                ],
              ),
            ),
          ),
          SafeArea(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const AppBackButtonHeader(),
                const SizedBox(height: AppBackButton.gapBelow),
                Expanded(
                  child: async.when(
                    loading: () => const Center(
                      child: CircularProgressIndicator(
                        color: AppColors.splashTextPrimary,
                      ),
                    ),
                    error: (error, _) => Center(
                      child: Padding(
                        padding: EdgeInsets.symmetric(horizontal: horizontal),
                        child: Text(
                          error is AppException
                              ? error.message
                              : 'Could not load media.',
                          textAlign: TextAlign.center,
                          style: const TextStyle(
                            color: AppColors.splashTextMuted,
                          ),
                        ),
                      ),
                    ),
                    data: (item) {
                      final canPlay = item.status == MediaStatus.completed;
                      return Align(
                        alignment: Alignment.topCenter,
                        child: ConstrainedBox(
                          constraints: const BoxConstraints(maxWidth: 520),
                          child: ListView(
                            padding: EdgeInsets.fromLTRB(
                              horizontal,
                              0,
                              horizontal,
                              AppSpacing.xxl,
                            ),
                            children: [
                              _MediaPreviewCard(
                                item: item,
                                canPlay: canPlay,
                                onPlay: () => _onPlay(item),
                              ),
                              const SizedBox(height: AppSpacing.sm),
                              Text(
                                'Status: ${item.status.name}',
                                style: TextStyle(
                                  fontSize: 13,
                                  color: AppColors.splashTextMuted
                                      .withValues(alpha: 0.95),
                                ),
                              ),
                              if (item.errorMessage != null) ...[
                                const SizedBox(height: AppSpacing.xs),
                                Text(
                                  item.errorMessage!,
                                  style: const TextStyle(
                                    fontSize: 13,
                                    color: AppColors.statusFailed,
                                  ),
                                ),
                              ],
                              if (item.status == MediaStatus.failed) ...[
                                const SizedBox(height: AppSpacing.sm),
                                TextButton(
                                  onPressed: _onRetry,
                                  child: const Text('Retry download'),
                                ),
                              ],
                              const SizedBox(height: AppSpacing.md),
                              _MediaInfoCard(
                                item: item,
                                onShare: () => unawaited(_onShare(item)),
                                onDelete: _onDelete,
                              ),
                            ],
                          ),
                        ),
                      );
                    },
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

class _MediaPreviewCard extends StatelessWidget {
  const _MediaPreviewCard({
    required this.item,
    required this.canPlay,
    required this.onPlay,
  });

  final MediaItemPreview item;
  final bool canPlay;
  final VoidCallback onPlay;

  LinearGradient get _coverGradient {
    return switch (item.platform) {
      MediaPlatform.instagram => LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [
            AppColors.brandOrangeDeep.withValues(alpha: 0.85),
            AppColors.splashBgMahogany,
            AppColors.splashBgNavy,
          ],
        ),
      MediaPlatform.facebook => LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [
            AppColors.statusQueued.withValues(alpha: 0.75),
            AppColors.splashBgNavy,
            AppColors.splashBgDeep,
          ],
        ),
    };
  }

  @override
  Widget build(BuildContext context) {
    final isIg = item.platform == MediaPlatform.instagram;

    return AspectRatio(
      aspectRatio: 3 / 4,
      child: ClipRRect(
        borderRadius: AppRadius.circularXxxl,
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: canPlay ? onPlay : null,
            borderRadius: AppRadius.circularXxxl,
            child: Ink(
              decoration: BoxDecoration(
                borderRadius: AppRadius.circularXxxl,
                gradient: _coverGradient,
                border: Border.all(
                  color: AppColors.splashChipBorder.withValues(alpha: 0.4),
                ),
                boxShadow: AppShadows.cta,
              ),
              child: Stack(
                fit: StackFit.expand,
                children: [
                  if (item.hasThumbnailUrl)
                    Positioned.fill(
                      child: Image.network(
                        item.thumbnailUrl!,
                        fit: BoxFit.cover,
                        errorBuilder: (context, error, stackTrace) =>
                            const SizedBox.shrink(),
                      ),
                    ),
                  Positioned(
                    top: AppSpacing.md,
                    left: AppSpacing.md,
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: AppSpacing.sm,
                        vertical: AppSpacing.xs,
                      ),
                      decoration: BoxDecoration(
                        color: AppColors.splashBgDeep.withValues(alpha: 0.45),
                        borderRadius: AppRadius.circularPill,
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          isIg
                              ? const InstagramIcon(size: 14)
                              : const Icon(
                                  Icons.facebook,
                                  size: 14,
                                  color: AppColors.splashTextPrimary,
                                ),
                          const SizedBox(width: AppSpacing.xs),
                          Text(
                            item.platform.label,
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: AppColors.splashTextPrimary,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  Center(
                    child: Container(
                      width: 64,
                      height: 64,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color:
                            AppColors.splashTextPrimary.withValues(alpha: 0.22),
                        border: Border.all(
                          color: AppColors.splashTextPrimary
                              .withValues(alpha: 0.55),
                        ),
                      ),
                      child: const Icon(
                        Icons.play_arrow_rounded,
                        size: 36,
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
    );
  }
}

class _MediaInfoCard extends StatelessWidget {
  const _MediaInfoCard({
    required this.item,
    required this.onShare,
    required this.onDelete,
  });

  final MediaItemPreview item;
  final VoidCallback onShare;
  final VoidCallback onDelete;

  String _relativeSavedLabel(DateTime savedAt) {
    final now = DateTime.now();
    final diff = now.difference(savedAt.toLocal());
    if (diff.inDays >= 1) {
      final days = diff.inDays;
      return 'Saved $days ${days == 1 ? 'day' : 'days'} ago';
    }
    if (diff.inHours >= 1) {
      final hours = diff.inHours;
      return 'Saved $hours ${hours == 1 ? 'hour' : 'hours'} ago';
    }
    final minutes = diff.inMinutes.clamp(1, 59);
    return 'Saved $minutes ${minutes == 1 ? 'minute' : 'minutes'} ago';
  }

  @override
  Widget build(BuildContext context) {
    final sizeLabel = item.fileSizeLabel ?? '—';
    final meta =
        '${item.platform.label} · ${_relativeSavedLabel(item.savedAt)} · $sizeLabel';

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(AppSpacing.cardPadding),
      decoration: BoxDecoration(
        color: AppColors.splashSheet.withValues(alpha: 0.92),
        borderRadius: AppRadius.circularCard,
        border: Border.all(
          color: AppColors.splashChipBorder.withValues(alpha: 0.7),
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            item.displayTitle,
            style: const TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.w700,
              letterSpacing: -0.3,
              color: AppColors.splashTextPrimary,
            ),
          ),
          const SizedBox(height: AppSpacing.xs),
          Text(
            meta,
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w400,
              height: 1.35,
              color: AppColors.splashTextMuted.withValues(alpha: 0.95),
            ),
          ),
          const SizedBox(height: AppSpacing.lg),
          Row(
            children: [
              Expanded(
                child: SizedBox(
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
                        onTap: onShare,
                        borderRadius: AppRadius.circularButton,
                        child: const Center(
                          child: Text(
                            'Share',
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w700,
                              color: AppColors.splashTextPrimary,
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
              ),
              const SizedBox(width: AppSpacing.sm),
              Expanded(
                child: SizedBox(
                  height: AppSpacing.buttonHeight,
                  child: Material(
                    color: Colors.transparent,
                    child: InkWell(
                      onTap: onDelete,
                      borderRadius: AppRadius.circularButton,
                      child: Ink(
                        decoration: BoxDecoration(
                          color:
                              AppColors.splashChipFill.withValues(alpha: 0.65),
                          borderRadius: AppRadius.circularButton,
                          border: Border.all(
                            color: AppColors.splashChipBorder.withValues(
                              alpha: 0.9,
                            ),
                          ),
                        ),
                        child: const Center(
                          child: Text(
                            'Delete',
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.w700,
                              color: AppColors.splashTextPrimary,
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
