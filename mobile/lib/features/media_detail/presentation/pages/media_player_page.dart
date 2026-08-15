import 'dart:async';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:path/path.dart' as p;
import 'package:video_player/video_player.dart';

import '../../../../core/errors/app_exception.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_platform.dart';
import '../../../../shared/models/media_status.dart';
import '../../../../shared/widgets/instagram_icon.dart';
import '../../../media/data/gallery/gallery_export_service.dart';
import '../../../media/data/gallery/gallery_export_store.dart';
import '../../../media/presentation/providers/gallery_export_providers.dart';
import '../../../media/presentation/providers/media_providers.dart';
import '../../data/media_share_service.dart';
import '../../domain/related_reels.dart';

/// Full-screen in-app player for completed media (SRS FR-014 / AC-09).
///
/// The video viewport is a vertical snap-paged reel feed: same-platform
/// completed reels (including the opened reel), then the other platform.
class MediaPlayerPage extends ConsumerStatefulWidget {
  const MediaPlayerPage({super.key, required this.mediaId});

  final String mediaId;

  @override
  ConsumerState<MediaPlayerPage> createState() => _MediaPlayerPageState();
}

class _MediaPlayerPageState extends ConsumerState<MediaPlayerPage> {
  @override
  Widget build(BuildContext context) {
    final currentAsync = ref.watch(mediaDetailProvider(widget.mediaId));
    final listAsync = ref.watch(mediaListProvider);
    final list = listAsync.asData?.value;
    MediaItemPreview? current;
    if (list != null) {
      for (final item in list) {
        if (item.id.trim() == widget.mediaId.trim()) {
          current = item;
          break;
        }
      }
    }
    current ??= currentAsync.asData?.value;

    return Scaffold(
      backgroundColor: AppColors.splashBgDeep,
      body: Stack(
        fit: StackFit.expand,
        children: [
          const DecoratedBox(
            decoration: BoxDecoration(gradient: AppGradients.splashBackground),
          ),
          if (current != null && current.status != MediaStatus.completed)
            const Center(
              child: Padding(
                padding: EdgeInsets.all(AppSpacing.lg),
                child: Text(
                  'Playback is only available for completed media.',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: AppColors.splashTextPrimary,
                    fontSize: 15,
                  ),
                ),
              ),
            )
          else if (current != null)
            _ReelPagedViewport(
              key: ValueKey<String>('feed-${current.id}'),
              feed: buildVerticalReelFeed(
                current: current,
                all: list ?? const <MediaItemPreview>[],
              ),
            )
          else if (currentAsync.hasError)
            Center(
              child: Padding(
                padding: const EdgeInsets.all(AppSpacing.lg),
                child: Text(
                  currentAsync.error is AppException
                      ? (currentAsync.error as AppException).message
                      : 'Could not load media.',
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    color: AppColors.splashTextPrimary,
                    fontSize: 15,
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _ReelPagedViewport extends StatefulWidget {
  const _ReelPagedViewport({super.key, required this.feed});

  final List<MediaItemPreview> feed;

  @override
  State<_ReelPagedViewport> createState() => _ReelPagedViewportState();
}

class _ReelPagedViewportState extends State<_ReelPagedViewport> {
  late final PageController _pageController;
  final _LocalPlaybackSession _session = _LocalPlaybackSession();
  var _page = 0;

  @override
  void initState() {
    super.initState();
    _pageController = PageController();
    _preloadAround(_page);
  }

  @override
  void didUpdateWidget(covariant _ReelPagedViewport oldWidget) {
    super.didUpdateWidget(oldWidget);
    _preloadAround(_page);
  }

  @override
  void dispose() {
    _session.dispose();
    _pageController.dispose();
    super.dispose();
  }

  void _preloadAround(int page) {
    unawaited(
      _session.preloadAround(widget.feed, page).then((_) {
        if (mounted) setState(() {});
      }),
    );
  }

  @override
  Widget build(BuildContext context) {
    final system = MediaQuery.paddingOf(context);
    return Padding(
      padding: EdgeInsets.only(top: system.top, bottom: system.bottom),
      child: PageView.builder(
        controller: _pageController,
        scrollDirection: Axis.vertical,
        pageSnapping: true,
        physics: const PageScrollPhysics(parent: ClampingScrollPhysics()),
        itemCount: widget.feed.length,
        onPageChanged: (index) {
          setState(() => _page = index);
          _preloadAround(index);
        },
        itemBuilder: (context, index) {
          final item = widget.feed[index];
          return _PagedReelPlayer(
            key: ValueKey<String>('reel-${item.id}'),
            session: _session,
            item: item,
            mediaId: item.id,
            isActive: index == _page,
          );
        },
      ),
    );
  }
}

/// Feed-session cache: initialized controllers reused until the player closes.
class _LocalPlaybackSession {
  final Map<String, VideoPlayerController> _ready =
      <String, VideoPlayerController>{};
  final Map<String, Future<VideoPlayerController?>> _inflight =
      <String, Future<VideoPlayerController?>>{};

  VideoPlayerController? peek(String mediaId) {
    final controller = _ready[mediaId];
    if (controller == null || !controller.value.isInitialized) return null;
    return controller;
  }

  void adopt(String mediaId, VideoPlayerController controller) {
    _ready[mediaId] = controller;
  }

  Future<void> _initChain = Future<void>.value();

  Future<VideoPlayerController?> ensureLocal(String mediaId) {
    final existing = peek(mediaId);
    if (existing != null) return Future<VideoPlayerController?>.value(existing);
    return _inflight.putIfAbsent(mediaId, () {
      final previous = _initChain;
      final done = Completer<void>();
      _initChain = done.future;
      return () async {
        VideoPlayerController? opened;
        try {
          await previous;
          final ready = peek(mediaId);
          if (ready != null) return ready;

          opened = await _openLocalPlayableController(mediaId);
          if (opened == null) return null;
          if (!opened.value.isInitialized) {
            try {
              await opened.initialize();
            } catch (error) {
              // contentUri may already be initialized by the gallery lookup.
              if (!opened.value.isInitialized) rethrow;
              debugPrint('LOCAL_PRELOAD init skipped: $error');
            }
          }
          await opened.setLooping(true);
          _ready[mediaId] = opened;
          return opened;
        } catch (error) {
          debugPrint('LOCAL_PRELOAD: $error');
          if (opened != null && opened.value.isInitialized) {
            _ready[mediaId] = opened;
            return opened;
          }
          if (opened != null) {
            await _discardUnusableShareCache(opened);
            await opened.dispose();
          }
          return null;
        } finally {
          if (!done.isCompleted) done.complete();
          _inflight.remove(mediaId);
        }
      }();
    });
  }

  Future<void> preloadAround(List<MediaItemPreview> feed, int page) async {
    // Current reel first so swipe-open never loses a decoder slot to neighbours.
    for (final offset in const [0, -1, 1]) {
      final index = page + offset;
      if (index < 0 || index >= feed.length) continue;
      await ensureLocal(feed[index].id);
    }
  }

  void dispose() {
    for (final controller in _ready.values) {
      controller.dispose();
    }
    _ready.clear();
    _inflight.clear();
  }
}

/// Existing local copies: share cache, gallery temp cache, Task 1 MediaStore.
Future<VideoPlayerController?> _openLocalPlayableController(
  String mediaId,
) async {
  final id = mediaId.trim();
  if (id.isEmpty) return null;
  const channel = MethodChannel('com.example.mobile/share_intent');

  try {
    final cached = await MediaShareService().findExistingShareCache(mediaId: id);
    if (cached != null) {
      return VideoPlayerController.file(
        cached,
        videoPlayerOptions: VideoPlayerOptions(mixWithOthers: false),
      );
    }
  } catch (error) {
    debugPrint('LOCAL_PLAYBACK cache: $error');
  }

  try {
    final cacheDir = await channel.invokeMethod<String>('getCacheDir');
    if (cacheDir != null && cacheDir.isNotEmpty) {
      final fromGalleryTemp =
          await _controllerFromGalleryTempCache(cacheDir, id);
      if (fromGalleryTemp != null) return fromGalleryTemp;
    }
  } catch (error) {
    debugPrint('LOCAL_PLAYBACK cache: $error');
  }

  if (!Platform.isAndroid) return null;

  try {
    final fromGallery = await _controllerFromGalleryLookup(channel, id);
    if (fromGallery != null) return fromGallery;

    // Do not treat DB Completed as local. Retry only when Task 1 actually
    // marked this id as exported and MediaStore may still be catching up.
    var exported = false;
    try {
      exported = await GalleryExportStore().isExported(id);
    } catch (_) {
      exported = false;
    }
    if (exported) {
      for (var attempt = 0; attempt < 4; attempt++) {
        await Future<void>.delayed(const Duration(milliseconds: 150));
        final retried = await _controllerFromGalleryLookup(channel, id);
        if (retried != null) return retried;
      }
    }
  } catch (error) {
    debugPrint('LOCAL_PLAYBACK gallery: $error');
  }
  return null;
}

Future<VideoPlayerController?> _controllerFromGalleryTempCache(
  String cacheDir,
  String id,
) async {
  final safeId = id.replaceAll(RegExp(r'[^\w\-]+'), '_');
  final names = <String>{
    'srs-gallery-$safeId.mp4',
    'srs-gallery-$safeId.mov',
    'srs-gallery-$id.mp4',
    'srs-gallery-$id.mov',
  };
  for (final name in names) {
    final file = File(p.join(cacheDir, name));
    if (await file.exists() && await file.length() > 0) {
      return VideoPlayerController.file(
        file,
        videoPlayerOptions: VideoPlayerOptions(mixWithOthers: false),
      );
    }
  }
  return null;
}

Future<void> _discardUnusableShareCache(
  VideoPlayerController controller,
) async {
  final source = controller.dataSource;
  if (!source.contains('srs-share-') || source.toLowerCase().endsWith('.tmp')) {
    return;
  }
  try {
    var path = source;
    if (path.startsWith('file:')) {
      path = Uri.parse(path).toFilePath();
    }
    final file = File(path);
    if (await file.exists()) {
      await file.delete();
    }
  } catch (_) {}
}

Future<VideoPlayerController?> _controllerFromGalleryLookup(
  MethodChannel channel,
  String id,
) async {
  for (final token in _galleryLookupTokens(id)) {
    final uri = await channel.invokeMethod<String>(
      'findGalleryVideoUri',
      <String, Object?>{'mediaIdToken': token},
    );
    final raw = uri?.trim() ?? '';
    if (raw.isEmpty) continue;
    final opened = await _controllerFromGalleryUri(raw);
    if (opened != null) return opened;
  }
  return null;
}

/// Task 1 writes `external_primary`; some lookups return `external`. Try both.
Future<VideoPlayerController?> _controllerFromGalleryUri(String raw) async {
  for (final uri in _playbackContentUris(raw)) {
    final controller = VideoPlayerController.contentUri(
      uri,
      videoPlayerOptions: VideoPlayerOptions(mixWithOthers: false),
    );
    try {
      await controller.initialize();
      return controller;
    } catch (error) {
      debugPrint('LOCAL_PLAYBACK gallery init: $error');
      await controller.dispose();
    }
  }
  return null;
}

List<Uri> _playbackContentUris(String raw) {
  final original = Uri.tryParse(raw);
  if (original == null) return const <Uri>[];
  final uris = <Uri>[original];
  void add(String next) {
    if (next == raw) return;
    final uri = Uri.tryParse(next);
    if (uri != null && !uris.contains(uri)) uris.add(uri);
  }

  add(
    raw.replaceFirst(
      '/external/video/media/',
      '/external_primary/video/media/',
    ),
  );
  add(
    raw.replaceFirst(
      '/external_primary/video/media/',
      '/external/video/media/',
    ),
  );
  return uris;
}

List<String> _galleryLookupTokens(String mediaId) {
  final trimmed = mediaId.trim();
  final sanitized = MediaShareService.galleryMediaIdToken(trimmed);
  final tokens = <String>[
    sanitized,
    trimmed,
    sanitized.replaceAll('-', '_'),
    trimmed.replaceAll('-', '_'),
    sanitized.replaceAll('-', ''),
  ];
  return tokens
      .map((token) => token.trim())
      .where((token) => token.isNotEmpty)
      .toSet()
      .toList(growable: false);
}

class _PagedReelPlayer extends ConsumerStatefulWidget {
  const _PagedReelPlayer({
    super.key,
    required this.session,
    required this.item,
    required this.mediaId,
    required this.isActive,
  });

  final _LocalPlaybackSession session;
  final MediaItemPreview item;
  final String mediaId;
  final bool isActive;

  @override
  ConsumerState<_PagedReelPlayer> createState() => _PagedReelPlayerState();
}

class _PagedReelPlayerState extends ConsumerState<_PagedReelPlayer>
    with AutomaticKeepAliveClientMixin {
  VideoPlayerController? _controller;
  var _awaitingNetwork = false;
  var _loadStarted = false;
  String? _error;
  var _showControls = true;
  var _exporting = false;

  @override
  bool get wantKeepAlive => true;

  @override
  void initState() {
    super.initState();
    _bindSessionController();
    // Inactive pages must not initialize in parallel with the active reel.
    // Adjacent local preload stays on the session (_preloadAround).
    if (widget.isActive) {
      _loadPlayback();
    }
  }

  @override
  void didUpdateWidget(covariant _PagedReelPlayer oldWidget) {
    super.didUpdateWidget(oldWidget);
    _bindSessionController();
    if (widget.isActive && !oldWidget.isActive) {
      if (_controller != null && _controller!.value.isInitialized) {
        _controller!.play();
      } else {
        _loadPlayback();
      }
    } else if (!widget.isActive && oldWidget.isActive) {
      _controller?.pause();
    }
  }

  @override
  void dispose() {
    _controller?.removeListener(_onControllerUpdate);
    super.dispose();
  }

  void _onControllerUpdate() {
    if (!mounted) return;
    setState(() {});
  }

  void _bindSessionController() {
    final existing = widget.session.peek(widget.mediaId);
    if (existing == null || identical(_controller, existing)) return;
    _controller?.removeListener(_onControllerUpdate);
    _controller = existing;
    existing.addListener(_onControllerUpdate);
    _loadStarted = true;
    _awaitingNetwork = false;
    _error = null;
    unawaited(existing.setLooping(true));
    if (widget.isActive && existing.value.isInitialized) {
      existing.play();
    }
  }

  Future<void> _loadPlayback() async {
    final peeked = widget.session.peek(widget.mediaId);
    if (peeked != null) {
      if (!mounted) return;
      setState(() {
        _bindSessionController();
      });
      return;
    }

    if (_loadStarted && _controller != null) return;
    _loadStarted = true;

    try {
      final local = await widget.session.ensureLocal(widget.mediaId);
      if (!mounted) return;
      if (local != null) {
        setState(() {
          _awaitingNetwork = false;
          _error = null;
          _bindSessionController();
        });
        return;
      }

      // One more peek after the serialized local lookup — a neighbour init
      // that finished this reel must not fall through to VPS.
      final lateLocal = widget.session.peek(widget.mediaId);
      if (lateLocal != null) {
        if (!mounted) return;
        setState(() {
          _awaitingNetwork = false;
          _error = null;
          _bindSessionController();
        });
        return;
      }

      if (!widget.isActive) {
        _loadStarted = false;
        return;
      }

      setState(() {
        _awaitingNetwork = true;
        _error = null;
      });

      final item = await ref.read(mediaDetailProvider(widget.mediaId).future);
      if (item.status != MediaStatus.completed) {
        throw AppException(
          message: 'Playback is only available for completed media.',
        );
      }

      final playback = await ref
          .read(mediaRepositoryProvider)
          .getPlayback(item.id);
      final rawUrl = playback.playbackUrl?.trim();
      if (rawUrl == null || rawUrl.isEmpty) {
        throw const AppException(message: 'Playback URL unavailable.');
      }

      final mime = (playback.mimeType ?? 'video/mp4').split(';').first.trim();
      final cachedFile = await MediaShareService().cacheSignedPlayback(
        mediaId: item.id,
        rawPlaybackUrl: rawUrl,
        mimeType: mime.isEmpty ? 'video/mp4' : mime,
      );

      final attached = await _attachNetworkController(
        VideoPlayerController.file(
          cachedFile,
          videoPlayerOptions: VideoPlayerOptions(mixWithOthers: false),
        ),
      );
      if (!attached && mounted) {
        setState(() {
          _awaitingNetwork = false;
          _error ??= 'Unable to play this video.';
        });
      }
    } on AppException catch (error) {
      if (!mounted) return;
      setState(() {
        _awaitingNetwork = false;
        _error = error.message;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _awaitingNetwork = false;
        _error = 'Could not start playback.';
      });
      debugPrint('PLAYBACK_ERROR: $error');
    }
  }

  Future<bool> _attachNetworkController(
    VideoPlayerController controller,
  ) async {
    _controller?.removeListener(_onControllerUpdate);
    _controller = controller;
    controller.addListener(_onControllerUpdate);

    try {
      await controller.initialize();
      if (!mounted) return false;
      await controller.setLooping(true);
      widget.session.adopt(widget.mediaId, controller);
      if (widget.isActive) {
        await controller.play();
      }
      if (!mounted) return false;
      setState(() => _awaitingNetwork = false);
      return true;
    } catch (error) {
      debugPrint('VIDEO_PLAYER_INIT: $error');
      controller.removeListener(_onControllerUpdate);
      await controller.dispose();
      if (!mounted) return false;
      if (identical(_controller, controller)) {
        _controller = null;
      }
      return false;
    }
  }

  Future<void> _togglePlayPause() async {
    final controller = _controller;
    if (controller == null || !controller.value.isInitialized) return;
    if (controller.value.isPlaying) {
      await controller.pause();
    } else {
      await controller.play();
    }
  }

  void _snack(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message)),
    );
  }

  Future<void> _onDownload() async {
    if (!widget.isActive || _exporting) return;
    setState(() => _exporting = true);
    try {
      final outcome = await ref
          .read(galleryExportServiceProvider)
          .exportOne(widget.item);
      if (!mounted) return;
      switch (outcome) {
        case GalleryExportOutcome.exported:
          _snack('Saved to Gallery');
        case GalleryExportOutcome.alreadyExported:
          _snack('Already in Gallery');
        case GalleryExportOutcome.failed:
          _snack('Could not save to Gallery');
      }
    } finally {
      if (mounted) setState(() => _exporting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    super.build(context);
    final controller = _controller;
    final value = controller?.value;
    final isReady = value != null && value.isInitialized && _error == null;

    return SizedBox.expand(
      child: LayoutBuilder(
        builder: (context, constraints) {
          Widget? video;
          if (isReady) {
            final aspect = _sourceAspectRatio(value);
            var width = constraints.maxWidth;
            var height = width / aspect;
            if (height > constraints.maxHeight && constraints.maxHeight > 0) {
              height = constraints.maxHeight;
              width = height * aspect;
            }
            const nineSixteen = 9 / 16;
            if (aspect > 0 && (aspect - nineSixteen).abs() <= 0.03) {
              width = constraints.maxWidth;
              height = constraints.maxHeight;
            }
            video = Center(
              child: GestureDetector(
                onTap: () {
                  setState(() => _showControls = !_showControls);
                },
                child: SizedBox(
                width: width,
                height: height,
                child: ColoredBox(
                  color: Colors.black,
                  child: Stack(
                    fit: StackFit.expand,
                    children: [
                      VideoPlayer(controller!),
                      Positioned(
                        top: AppSpacing.md,
                        left: AppSpacing.md,
                        child: _ReelPlatformBadge(
                          platform: widget.item.platform,
                        ),
                      ),
                      if (_showControls)
                        Positioned(
                          left: AppSpacing.md,
                          right: AppSpacing.md,
                          bottom: AppSpacing.md,
                          child: _PlayerControls(
                            isPlaying: value.isPlaying,
                            position: value.position,
                            duration: value.duration,
                            exporting: _exporting,
                            onPlayPause: _togglePlayPause,
                            onDownload: _onDownload,
                            onSeek: (position) async {
                              await controller.seekTo(position);
                            },
                          ),
                        ),
                    ],
                  ),
                ),
              ),
            ),
            );
          }

          return Stack(
            fit: StackFit.expand,
            alignment: Alignment.center,
            children: [
              ?video,
              if (_awaitingNetwork && widget.isActive && !isReady)
                const Center(
                  child: CircularProgressIndicator(
                    color: AppColors.splashTextPrimary,
                  ),
                ),
              if (_error != null)
                Padding(
                  padding: const EdgeInsets.all(AppSpacing.lg),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        _error!,
                        textAlign: TextAlign.center,
                        style: const TextStyle(
                          color: AppColors.splashTextPrimary,
                          fontSize: 15,
                        ),
                      ),
                      const SizedBox(height: AppSpacing.md),
                      TextButton(
                        onPressed: () {
                          _loadStarted = false;
                          _loadPlayback();
                        },
                        child: const Text('Retry'),
                      ),
                    ],
                  ),
                ),
            ],
          );
        },
      ),
    );
  }

  /// Source aspect for contain-fit. Never forces 9:16.
  ///
  /// Texture Android reports encoded size plus [VideoPlayerValue.rotationCorrection].
  /// [VideoPlayer] already applies that via [RotatedBox]. A 16:9 file stored as
  /// a portrait buffer with 90° metadata must invert a portrait-reported aspect
  /// so the layout box is landscape. Do not invert when the reported size is
  /// already landscape (that would 9:16-stretch it).
  static double _sourceAspectRatio(VideoPlayerValue value) {
    final size = value.size;
    var aspect = size.width > 0 && size.height > 0
        ? size.width / size.height
        : value.aspectRatio;
    if (aspect <= 0) return 1.0;
    final turns = ((value.rotationCorrection % 360) + 360) % 360;
    if ((turns == 90 || turns == 270) && aspect < 1) {
      return 1 / aspect;
    }
    return aspect;
  }
}

class _ReelPlatformBadge extends StatelessWidget {
  const _ReelPlatformBadge({required this.platform});

  final MediaPlatform platform;

  @override
  Widget build(BuildContext context) {
    final isIg = platform == MediaPlatform.instagram;
    return IgnorePointer(
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
              platform.label,
              style: const TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w600,
                color: AppColors.splashTextPrimary,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _PlayerControls extends StatelessWidget {
  const _PlayerControls({
    required this.isPlaying,
    required this.position,
    required this.duration,
    required this.exporting,
    required this.onPlayPause,
    required this.onDownload,
    required this.onSeek,
  });

  final bool isPlaying;
  final Duration position;
  final Duration duration;
  final bool exporting;
  final VoidCallback onPlayPause;
  final VoidCallback onDownload;
  final ValueChanged<Duration> onSeek;

  String _format(Duration value) {
    final total = value.inSeconds;
    final m = (total ~/ 60).toString().padLeft(2, '0');
    final s = (total % 60).toString().padLeft(2, '0');
    return '$m:$s';
  }

  @override
  Widget build(BuildContext context) {
    final maxMs = duration.inMilliseconds <= 0 ? 1 : duration.inMilliseconds;
    final value = position.inMilliseconds.clamp(0, maxMs).toDouble();

    return DecoratedBox(
      decoration: BoxDecoration(
        color: AppColors.splashBgDeep.withValues(alpha: 0.72),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 8, 12, 8),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              children: [
                IconButton(
                  onPressed: onPlayPause,
                  icon: Icon(
                    isPlaying ? Icons.pause_rounded : Icons.play_arrow_rounded,
                    color: AppColors.splashTextPrimary,
                  ),
                ),
                Text(
                  '${_format(position)} / ${_format(duration)}',
                  style: const TextStyle(
                    color: AppColors.splashTextMuted,
                    fontSize: 12,
                  ),
                ),
                const Spacer(),
                IconButton(
                  onPressed: exporting ? null : onDownload,
                  icon: exporting
                      ? const SizedBox(
                          width: 22,
                          height: 22,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: AppColors.splashTextPrimary,
                          ),
                        )
                      : const Icon(
                          Icons.download_rounded,
                          color: AppColors.splashTextPrimary,
                        ),
                ),
              ],
            ),
            SliderTheme(
              data: SliderTheme.of(context).copyWith(
                trackHeight: 2,
                thumbShape: const RoundSliderThumbShape(enabledThumbRadius: 6),
              ),
              child: Slider(
                value: value,
                max: maxMs.toDouble(),
                activeColor: AppColors.splashTextPrimary,
                inactiveColor: AppColors.splashChipBorder,
                onChanged: (v) => onSeek(Duration(milliseconds: v.round())),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
