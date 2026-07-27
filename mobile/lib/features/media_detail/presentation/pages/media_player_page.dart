import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:video_player/video_player.dart';

import '../../../../core/errors/app_exception.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../../../shared/models/media_status.dart';
import '../../../../shared/widgets/app_back_button.dart';
import '../../../media/presentation/providers/media_providers.dart';

/// Full-screen in-app player for completed media (SRS FR-014 / AC-09).
class MediaPlayerPage extends ConsumerStatefulWidget {
  const MediaPlayerPage({super.key, required this.mediaId});

  final String mediaId;

  @override
  ConsumerState<MediaPlayerPage> createState() => _MediaPlayerPageState();
}

class _MediaPlayerPageState extends ConsumerState<MediaPlayerPage> {
  VideoPlayerController? _controller;
  var _loadingUrl = true;
  var _initializingPlayer = false;
  String? _error;
  var _showControls = true;

  @override
  void initState() {
    super.initState();
    _loadPlayback();
  }

  @override
  void dispose() {
    _controller?.removeListener(_onControllerUpdate);
    _controller?.dispose();
    super.dispose();
  }

  void _onControllerUpdate() {
    if (!mounted) return;
    setState(() {});
  }

  Future<void> _loadPlayback() async {
    setState(() {
      _loadingUrl = true;
      _error = null;
    });

    try {
      final item =
          await ref.read(mediaDetailProvider(widget.mediaId).future);
      if (item.status != MediaStatus.completed) {
        throw AppException(
          message: 'Playback is only available for completed media.',
        );
      }

      final playback =
          await ref.read(mediaRepositoryProvider).getPlayback(item.id);
      final url = playback.playbackUrl?.trim();
      if (url == null || url.isEmpty) {
        throw const AppException(message: 'Playback URL unavailable.');
      }

      await _attachController(Uri.parse(url));
    } on AppException catch (error) {
      if (!mounted) return;
      setState(() {
        _loadingUrl = false;
        _error = error.message;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _loadingUrl = false;
        _error = 'Could not start playback.';
      });
    }
  }

  Future<void> _attachController(Uri uri) async {
    final previous = _controller;
    previous?.removeListener(_onControllerUpdate);
    await previous?.dispose();

    final controller = VideoPlayerController.networkUrl(uri);
    _controller = controller;
    controller.addListener(_onControllerUpdate);

    setState(() {
      _loadingUrl = false;
      _initializingPlayer = true;
      _error = null;
    });

    try {
      await controller.initialize();
      await controller.play();
      if (!mounted) return;
      setState(() => _initializingPlayer = false);
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _initializingPlayer = false;
        _error = 'Unable to play this video.';
      });
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

  @override
  Widget build(BuildContext context) {
    final controller = _controller;
    final value = controller?.value;
    final isReady = value != null && value.isInitialized && _error == null;
    final isBuffering = value?.isBuffering == true || _initializingPlayer;

    return Scaffold(
      backgroundColor: AppColors.splashBgDeep,
      body: Stack(
        fit: StackFit.expand,
        children: [
          const DecoratedBox(
            decoration: BoxDecoration(gradient: AppGradients.splashBackground),
          ),
          SafeArea(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const AppBackButtonHeader(),
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                      horizontal: AppSpacing.md,
                      vertical: AppSpacing.sm,
                    ),
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(24),
                      child: ColoredBox(
                        color: Colors.black.withValues(alpha: 0.55),
                        child: Stack(
                          alignment: Alignment.center,
                          children: [
                            if (isReady)
                              GestureDetector(
                                onTap: () {
                                  setState(() => _showControls = !_showControls);
                                },
                                child: AspectRatio(
                                  aspectRatio: value.aspectRatio == 0
                                      ? 9 / 16
                                      : value.aspectRatio,
                                  child: VideoPlayer(controller!),
                                ),
                              ),
                            if (_loadingUrl || isBuffering)
                              const CircularProgressIndicator(
                                color: AppColors.splashTextPrimary,
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
                                      onPressed: _loadPlayback,
                                      child: const Text('Retry'),
                                    ),
                                  ],
                                ),
                              ),
                            if (isReady && _showControls && !isBuffering)
                              Positioned(
                                left: AppSpacing.md,
                                right: AppSpacing.md,
                                bottom: AppSpacing.md,
                                child: _PlayerControls(
                                  isPlaying: value.isPlaying,
                                  position: value.position,
                                  duration: value.duration,
                                  onPlayPause: _togglePlayPause,
                                  onSeek: (position) async {
                                    await controller!.seekTo(position);
                                  },
                                ),
                              ),
                          ],
                        ),
                      ),
                    ),
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

class _PlayerControls extends StatelessWidget {
  const _PlayerControls({
    required this.isPlaying,
    required this.position,
    required this.duration,
    required this.onPlayPause,
    required this.onSeek,
  });

  final bool isPlaying;
  final Duration position;
  final Duration duration;
  final VoidCallback onPlayPause;
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
                    isPlaying
                        ? Icons.pause_rounded
                        : Icons.play_arrow_rounded,
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
