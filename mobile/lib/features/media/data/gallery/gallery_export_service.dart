import 'dart:async';
import 'dart:io';
import 'dart:math' as math;

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:http/http.dart' as http;
import 'package:flutter/services.dart';
import 'package:path/path.dart' as p;

import '../../../../core/network/media_url_resolver.dart';
import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_status.dart';
import '../../domain/repositories/media_repository.dart';
import 'gallery_export_store.dart';

/// Result of a single gallery export attempt.
enum GalleryExportOutcome {
  /// MediaStore write succeeded (or already existed) and id was marked exported.
  exported,

  /// Skipped because SharedPreferences already marked this id.
  alreadyExported,

  /// Transient/permanent failure — caller should retry later (not marked).
  failed,
}

/// Bounded exponential backoff for failed gallery exports.
///
/// 15s → 30s → 60s → 120s → 240s → capped at 5 minutes.
Duration galleryExportBackoff(int failureCount) {
  final capped = failureCount.clamp(0, 5);
  final seconds = math.min(300, 15 * (1 << capped));
  return Duration(seconds: seconds);
}

/// Idle discovery interval when no pending exports are known.
const Duration galleryExportIdleDiscoverInterval = Duration(seconds: 45);

/// Max simultaneous gallery content streams (VPS bandwidth / RAM).
const int galleryExportMaxConcurrent = 1;

/// Page size used only by the gallery coordinator (Library pageSize unchanged).
const int galleryExportPageSize = 50;

/// Operations used by [GalleryExportCoordinator] (real or fake in tests).
abstract class GalleryExportOperations {
  bool get isAndroid;

  int get inFlightCount;

  /// Whether [mediaId] is currently being exported.
  bool isInFlight(String mediaId);

  Future<List<MediaItemPreview>> listAllCompletedMedia();

  Future<GalleryExportOutcome> exportOne(MediaItemPreview item);
}

/// Exports completed ReelBox media into the Android Gallery via MediaStore.
class GalleryExportService implements GalleryExportOperations {
  GalleryExportService({
    required MediaRepository mediaRepository,
    required GalleryExportStore store,
    MethodChannel? channel,
    http.Client? httpClient,
  })  : _repo = mediaRepository,
        // ignore: prefer_initializing_formals
        _store = store,
        _channel = channel ?? const MethodChannel(_channelName),
        _http = httpClient ?? http.Client();

  static const String _channelName = 'com.example.mobile/share_intent';
  static const String _methodGetCacheDir = 'getCacheDir';
  static const String _methodSaveVideoToGallery = 'saveVideoToGallery';
  static const String _relativePath = 'Movies/ReelBox';

  final MediaRepository _repo;
  final GalleryExportStore _store;
  final MethodChannel _channel;
  final http.Client _http;

  final Set<String> _inFlight = <String>{};

  /// Number of exports currently running (for concurrency tests / coordinator).
  @override
  int get inFlightCount => _inFlight.length;

  @override
  bool isInFlight(String mediaId) => _inFlight.contains(mediaId.trim());

  @override
  bool get isAndroid =>
      !kIsWeb && defaultTargetPlatform == TargetPlatform.android;

  /// Lists every Completed media item via paginated API calls (gallery-only).
  @override
  Future<List<MediaItemPreview>> listAllCompletedMedia() async {
    final all = <MediaItemPreview>[];
    var page = 1;
    var totalCount = 0;

    while (page <= 100) {
      final result = await _repo.listMedia(
        page: page,
        pageSize: galleryExportPageSize,
        status: 'completed',
      );
      totalCount = result.totalCount;
      for (final item in result.items) {
        if (item.status == MediaStatus.completed) {
          all.add(item);
        }
      }
      if (result.items.length < galleryExportPageSize) break;
      if (totalCount > 0 && all.length >= totalCount) break;
      page++;
    }
    return all;
  }

  /// Exports one completed item if not already exported.
  @override
  Future<GalleryExportOutcome> exportOne(MediaItemPreview item) async {
    if (!isAndroid) return GalleryExportOutcome.alreadyExported;

    final id = item.id.trim();
    if (id.isEmpty) return GalleryExportOutcome.alreadyExported;
    if (item.status != MediaStatus.completed) {
      return GalleryExportOutcome.failed;
    }
    if (_inFlight.contains(id)) return GalleryExportOutcome.failed;
    if (await _store.isExported(id)) {
      return GalleryExportOutcome.alreadyExported;
    }

    _inFlight.add(id);
    File? tempFile;
    try {
      final playback = await _repo.getPlayback(id);
      final rawUrl = playback.playbackUrl?.trim();
      if (rawUrl == null || rawUrl.isEmpty) {
        debugPrint('GALLERY_EXPORT: skip $id — missing playback URL');
        return GalleryExportOutcome.failed;
      }

      final mime = (playback.mimeType ?? item.mimeType ?? 'video/mp4')
          .split(';')
          .first
          .trim();
      final extension = galleryExtensionForMime(mime);
      final displayName = buildGalleryDisplayName(
        mediaId: id,
        title: item.title ?? item.displayTitle,
        extension: extension,
      );

      final uri = resolveSignedMediaUrl(rawUrl);
      tempFile = await _streamToTempFile(
        uri: uri,
        mediaId: id,
        extension: extension,
      );

      final saved = await _channel.invokeMethod<bool>(
        _methodSaveVideoToGallery,
        <String, Object?>{
          'path': tempFile.path,
          'displayName': displayName,
          'mimeType': mime.isEmpty ? 'video/mp4' : mime,
          'relativePath': _relativePath,
        },
      );

      if (saved == true) {
        await _store.markExported(id);
        debugPrint('GALLERY_EXPORT: saved $id → $_relativePath/$displayName');
        return GalleryExportOutcome.exported;
      }

      debugPrint('GALLERY_EXPORT: native save returned false for $id');
      return GalleryExportOutcome.failed;
    } on PlatformException catch (error, stack) {
      debugPrint(
        'GALLERY_EXPORT: platform failure for $id: ${error.code} ${error.message}',
      );
      debugPrint('$stack');
      return GalleryExportOutcome.failed;
    } catch (error, stack) {
      debugPrint('GALLERY_EXPORT: failure for $id: $error');
      debugPrint('$stack');
      return GalleryExportOutcome.failed;
    } finally {
      _inFlight.remove(id);
      if (tempFile != null) {
        try {
          if (await tempFile.exists()) {
            await tempFile.delete();
          }
        } catch (_) {}
      }
    }
  }

  Future<File> _streamToTempFile({
    required Uri uri,
    required String mediaId,
    required String extension,
  }) async {
    final cacheDir = await _channel.invokeMethod<String>(_methodGetCacheDir);
    if (cacheDir == null || cacheDir.isEmpty) {
      throw StateError('Could not access app cache for gallery export.');
    }

    final safeId = mediaId.replaceAll(RegExp(r'[^\w\-]+'), '_');
    final file = File(p.join(cacheDir, 'srs-gallery-$safeId$extension'));
    if (await file.exists()) {
      await file.delete();
    }

    final request = http.Request('GET', uri);
    final response = await _http.send(request);
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw HttpException(
        'Gallery export download failed (${response.statusCode})',
        uri: uri,
      );
    }

    final sink = file.openWrite();
    try {
      await response.stream.pipe(sink);
      await sink.flush();
    } catch (_) {
      await sink.close();
      rethrow;
    }
    await sink.close();

    final length = await file.length();
    if (length <= 0) {
      throw StateError('Gallery export downloaded empty file for $mediaId');
    }
    return file;
  }
}

/// App-scoped coordinator: discovers Completed media and exports with backoff.
///
/// Independent of Library/Home polling and MainShellScaffold lifecycle.
class GalleryExportCoordinator with WidgetsBindingObserver {
  GalleryExportCoordinator({
    required GalleryExportOperations service,
    required GalleryExportStore store,
    required Future<bool> Function() isSignedIn,
    this.maxConcurrent = galleryExportMaxConcurrent,
    this.idleDiscoverInterval = galleryExportIdleDiscoverInterval,
    this.now = DateTime.now,
  }) : _service = service, // ignore: prefer_initializing_formals
        _store = store, // ignore: prefer_initializing_formals
        _isSignedIn = isSignedIn; // ignore: prefer_initializing_formals

  final GalleryExportOperations _service;
  final GalleryExportStore _store;
  final Future<bool> Function() _isSignedIn;
  final int maxConcurrent;
  final Duration idleDiscoverInterval;
  final DateTime Function() now;

  Timer? _timer;
  bool _started = false;
  bool _reconcileInProgress = false;
  bool _disposed = false;

  final Map<String, int> _failureCounts = <String, int>{};
  final Map<String, DateTime> _nextAttemptAt = <String, DateTime>{};

  /// Completed IDs already observed via [onMediaListSnapshot] (kick dedupe).
  final Set<String> _seenCompletedFromList = <String>{};

  /// Test/inspection: ids currently waiting for backoff.
  Map<String, int> get failureCounts => Map.unmodifiable(_failureCounts);

  /// Test/inspection: completed IDs already seen from media list kicks.
  Set<String> get seenCompletedFromList =>
      Set.unmodifiable(_seenCompletedFromList);

  bool get isStarted => _started;

  void start({bool reconcileImmediately = true}) {
    if (_disposed || _started) return;
    _started = true;
    WidgetsBinding.instance.addObserver(this);
    if (reconcileImmediately) {
      unawaited(reconcile(reason: 'start'));
    }
  }

  void stop() {
    if (!_started) return;
    _started = false;
    _timer?.cancel();
    _timer = null;
    WidgetsBinding.instance.removeObserver(this);
  }

  void dispose() {
    stop();
    _disposed = true;
  }

  /// Call after login / cold start when session becomes available.
  void onAuthenticated() {
    if (_disposed) return;
    if (!_started) {
      start(reconcileImmediately: true);
    } else {
      unawaited(reconcile(reason: 'authenticated'));
    }
  }

  void onLoggedOut() {
    stop();
    _failureCounts.clear();
    _nextAttemptAt.clear();
    _seenCompletedFromList.clear();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (!_started || _disposed) return;
    if (state == AppLifecycleState.resumed) {
      unawaited(reconcile(reason: 'resumed'));
    }
  }

  /// Additive kick from existing [mediaListProvider] emissions.
  ///
  /// Only newly observed Completed IDs (not yet seen from the list) can trigger
  /// an immediate [reconcile]. Already-exported, in-flight, and backoff-gated
  /// IDs are ignored. Does not replace the 45s idle fallback.
  Future<void> onMediaListSnapshot(List<MediaItemPreview> items) async {
    if (_disposed || !_started) return;

    final nowTs = now();
    var shouldKick = false;

    for (final item in items) {
      if (item.status != MediaStatus.completed) continue;
      final id = item.id.trim();
      if (id.isEmpty) continue;

      final isNewFromList = _seenCompletedFromList.add(id);
      if (!isNewFromList) continue;
      if (await _store.isExported(id)) continue;
      if (_service.isInFlight(id)) continue;
      final gate = _nextAttemptAt[id];
      if (gate != null && gate.isAfter(nowTs)) continue;

      shouldKick = true;
      // Keep scanning to record other newly seen IDs, then kick once.
    }

    if (shouldKick) {
      debugPrint('GALLERY_EXPORT: media_list kick — new Completed candidate(s)');
      await reconcile(reason: 'media_list');
    }
  }

  Future<void> reconcile({String reason = 'manual'}) async {
    if (_disposed || !_started || _reconcileInProgress) return;
    _reconcileInProgress = true;
    var hasPending = false;

    try {
      if (!await _isSignedIn()) {
        debugPrint('GALLERY_EXPORT: reconcile($reason) skipped — not signed in');
        return;
      }

      if (!_service.isAndroid) return;

      final completed = await _service.listAllCompletedMedia();
      final due = <MediaItemPreview>[];
      final nowTs = now();

      for (final item in completed) {
        final id = item.id.trim();
        if (id.isEmpty) continue;
        if (await _store.isExported(id)) {
          _failureCounts.remove(id);
          _nextAttemptAt.remove(id);
          continue;
        }
        hasPending = true;
        final gate = _nextAttemptAt[id];
        if (gate != null && gate.isAfter(nowTs)) {
          continue;
        }
        due.add(item);
      }

      if (due.isEmpty) {
        debugPrint(
          'GALLERY_EXPORT: reconcile($reason) — '
          '${completed.length} completed, pending=$hasPending, due=0',
        );
        return;
      }

      debugPrint(
        'GALLERY_EXPORT: reconcile($reason) — exporting ${due.length} item(s)',
      );

      var index = 0;
      Future<void> worker() async {
        while (true) {
          if (index >= due.length) return;
          final item = due[index++];
          final id = item.id.trim();
          final outcome = await _service.exportOne(item);
          switch (outcome) {
            case GalleryExportOutcome.exported:
            case GalleryExportOutcome.alreadyExported:
              _failureCounts.remove(id);
              _nextAttemptAt.remove(id);
            case GalleryExportOutcome.failed:
              hasPending = true;
              final failures = (_failureCounts[id] ?? 0) + 1;
              _failureCounts[id] = failures;
              _nextAttemptAt[id] = nowTs.add(galleryExportBackoff(failures - 1));
          }
        }
      }

      final workers = List<Future<void>>.generate(
        math.min(maxConcurrent, due.length),
        (_) => worker(),
      );
      await Future.wait(workers);

      // Re-check pending after exports.
      hasPending = false;
      for (final item in completed) {
        final id = item.id.trim();
        if (id.isEmpty) continue;
        if (!await _store.isExported(id)) {
          hasPending = true;
          break;
        }
      }
    } catch (error, stack) {
      hasPending = true;
      debugPrint('GALLERY_EXPORT: reconcile($reason) error: $error');
      debugPrint('$stack');
    } finally {
      _reconcileInProgress = false;
      if (_started && !_disposed) {
        _scheduleNext(hasPending: hasPending);
      }
    }
  }

  void _scheduleNext({required bool hasPending}) {
    _timer?.cancel();
    final delay = hasPending
        ? _shortestPendingDelay()
        : idleDiscoverInterval;
    _timer = Timer(delay, () {
      unawaited(reconcile(reason: hasPending ? 'backoff' : 'idle_discover'));
    });
  }

  Duration _shortestPendingDelay() {
    final nowTs = now();
    Duration? shortest;
    for (final at in _nextAttemptAt.values) {
      final remaining = at.difference(nowTs);
      final clamped =
          remaining.isNegative ? const Duration(seconds: 1) : remaining;
      if (shortest == null || clamped < shortest) {
        shortest = clamped;
      }
    }
    return shortest ?? galleryExportBackoff(0);
  }
}
