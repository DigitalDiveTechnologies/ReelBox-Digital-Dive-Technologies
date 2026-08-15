import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:http/http.dart' as http;
import 'package:path/path.dart' as p;

import '../../../core/network/media_url_resolver.dart';
import '../../media/data/gallery/gallery_export_store.dart';
import '../../media/data/models/media_dto.dart';

/// Opens the native Android share sheet for a completed reel.
///
/// Fast paths (no VPS download):
/// 1. Existing `srs-share-$mediaId.*` app cache
/// 2. Existing Task 1 MediaStore/Gallery entry (`Movies/ReelBox/ReelBox_<id>…`)
///
/// Fallback: stream signed content URL to share cache, then FileProvider share.
class MediaShareService {
  MediaShareService({
    MethodChannel? channel,
    http.Client? httpClient,
    Future<String?> Function()? resolveCacheDir,
    Future<bool> Function({
      required String path,
      required String mimeType,
      required String text,
    })? shareFile,
    Future<bool> Function({
      required String mediaIdToken,
      required String mimeType,
      required String text,
    })? shareGalleryByMediaId,
  })  : _channel = channel ?? const MethodChannel(_channelName),
        _http = httpClient ?? http.Client(),
        _resolveCacheDir = resolveCacheDir, // ignore: prefer_initializing_formals
        _shareFile = shareFile, // ignore: prefer_initializing_formals
        _shareGalleryByMediaId =
            shareGalleryByMediaId; // ignore: prefer_initializing_formals

  static const String _channelName = 'com.example.mobile/share_intent';
  static const String _methodGetCacheDir = 'getCacheDir';
  static const String _methodShareFile = 'shareFile';
  static const String _methodShareGalleryByMediaId =
      'shareGalleryVideoByMediaId';

  final MethodChannel _channel;
  final http.Client _http;
  final Future<String?> Function()? _resolveCacheDir;
  final Future<bool> Function({
    required String path,
    required String mimeType,
    required String text,
  })?
  _shareFile;
  final Future<bool> Function({
    required String mediaIdToken,
    required String mimeType,
    required String text,
  })?
  _shareGalleryByMediaId;

  /// Stable share-cache file name for [mediaId] + [extension].
  static String shareCacheFileName(String mediaId, String extension) {
    final ext = extension.startsWith('.') ? extension : '.$extension';
    final safeId = mediaId.trim().replaceAll(RegExp(r'[^\w\-]+'), '_');
    return 'srs-share-$safeId$ext';
  }

  /// Token used in Task 1 Gallery display names: `ReelBox_<token>…`.
  static String galleryMediaIdToken(String mediaId) {
    final bare = buildGalleryDisplayName(mediaId: mediaId, title: null);
    const prefix = 'ReelBox_';
    if (!bare.startsWith(prefix)) return mediaId.trim();
    final withoutPrefix = bare.substring(prefix.length);
    final dot = withoutPrefix.lastIndexOf('.');
    if (dot <= 0) return withoutPrefix;
    return withoutPrefix.substring(0, dot);
  }

  static String extensionForMime(String? mimeType) {
    final mime = (mimeType ?? 'video/mp4')
        .split(';')
        .first
        .trim()
        .toLowerCase();
    return switch (mime) {
      'video/quicktime' => '.mov',
      'image/jpeg' => '.jpg',
      'image/png' => '.png',
      'image/webp' => '.webp',
      _ => '.mp4',
    };
  }

  /// Returns an existing non-empty share cache file, if any.
  Future<File?> findExistingShareCache({
    required String mediaId,
    String? preferredMime,
  }) async {
    final cacheDir = await _cacheDirPath();
    if (cacheDir == null || cacheDir.isEmpty) return null;

    final preferred = extensionForMime(preferredMime);
    final candidates = <String>{
      preferred,
      '.mp4',
      '.mov',
      '.jpg',
      '.png',
      '.webp',
    };

    for (final ext in candidates) {
      final file = File(p.join(cacheDir, shareCacheFileName(mediaId, ext)));
      if (await _isValidCacheFile(file)) return file;
    }
    return null;
  }

  static Future<bool> _isValidCacheFile(File file) async {
    final name = p.basename(file.path).toLowerCase();
    if (name.endsWith('.tmp')) return false;
    if (!await file.exists()) return false;
    final length = await file.length();
    if (length <= 0) {
      try {
        await file.delete();
      } catch (_) {}
      return false;
    }
    return true;
  }

  /// In-flight VPS → share-cache writes, shared by Player and Share.
  static final Map<String, Future<File>> _inflightCacheWrites =
      <String, Future<File>>{};

  /// Downloads signed VPS content into `srs-share-$id.*` if missing.
  ///
  /// Reuses an in-flight write for the same [mediaId]. Does not overwrite a
  /// valid cache file.
  Future<File> cacheSignedPlayback({
    required String mediaId,
    required String rawPlaybackUrl,
    required String mimeType,
  }) {
    final id = mediaId.trim();
    return _inflightCacheWrites.putIfAbsent(id, () async {
      try {
        final existing = await findExistingShareCache(
          mediaId: id,
          preferredMime: mimeType,
        );
        if (existing != null) return existing;

        final cacheDir = await _cacheDirPath();
        if (cacheDir == null || cacheDir.isEmpty) {
          throw const ShareDownloadException(
            'Could not access app cache for sharing.',
          );
        }

        final mime = mimeType.split(';').first.trim();
        final dest = File(
          p.join(cacheDir, shareCacheFileName(id, extensionForMime(mime))),
        );
        await _streamToFile(
          uri: resolveSignedMediaUrl(rawPlaybackUrl),
          destination: dest,
        );
        return dest;
      } finally {
        _inflightCacheWrites.remove(id);
      }
    });
  }

  /// Shares a completed reel, preferring local sources over network download.
  ///
  /// [resolvePlayback] is only invoked when no valid local share/Gallery source exists.
  Future<void> shareCompletedMedia({
    required String mediaId,
    required String displayTitle,
    required String fallbackMime,
    required Future<PlaybackDto> Function() resolvePlayback,
  }) async {
    if (kIsWeb) {
      throw const ShareDownloadException('Sharing is not supported on web.');
    }

    final mimeHint = fallbackMime.split(';').first.trim();
    final cached = await findExistingShareCache(
      mediaId: mediaId,
      preferredMime: mimeHint,
    );
    if (cached != null) {
      final mime = _mimeForPath(cached.path, mimeHint);
      await _invokeShareFile(
        path: cached.path,
        mimeType: mime,
        text: displayTitle,
      );
      return;
    }

    // Task 1 Gallery already wrote Movies/ReelBox — share MediaStore URI (no VPS).
    // Single lookup. A committed row is shareable immediately; if none exists
    // yet, fall through to VPS instead of waiting on MediaStore.
    final sharedFromGallery = await _tryShareFromGallery(
      mediaId: mediaId,
      mimeType: mimeHint.isEmpty ? 'video/mp4' : mimeHint,
      text: displayTitle,
    );
    if (sharedFromGallery) {
      return;
    }

    // MediaStore can lag a moment after Task 1 marks the id exported.
    // Retry only then so share does not wait on an in-progress export or VPS.
    var alreadyExported = false;
    try {
      alreadyExported = await GalleryExportStore().isExported(mediaId);
    } catch (_) {
      alreadyExported = false;
    }
    if (alreadyExported) {
      for (var attempt = 0; attempt < 4; attempt++) {
        await Future<void>.delayed(const Duration(milliseconds: 150));
        final retried = await _tryShareFromGallery(
          mediaId: mediaId,
          mimeType: mimeHint.isEmpty ? 'video/mp4' : mimeHint,
          text: displayTitle,
        );
        if (retried) return;
      }
    }

    final playback = await resolvePlayback();
    final rawUrl = playback.playbackUrl?.trim();
    if (rawUrl == null || rawUrl.isEmpty) {
      throw const MissingPlaybackUrlException();
    }

    final mime = (playback.mimeType ?? fallbackMime).split(';').first.trim();
    final cachedFile = await cacheSignedPlayback(
      mediaId: mediaId,
      rawPlaybackUrl: rawUrl,
      mimeType: mime.isEmpty ? 'video/mp4' : mime,
    );

    await _invokeShareFile(
      path: cachedFile.path,
      mimeType: mime.isEmpty ? 'video/mp4' : mime,
      text: displayTitle,
    );
  }

  /// Backward-compatible entry used by older call sites / tests.
  Future<void> shareDownloadedFile({
    required String mediaId,
    required String displayTitle,
    required PlaybackDto playback,
    required String fallbackMime,
  }) {
    return shareCompletedMedia(
      mediaId: mediaId,
      displayTitle: displayTitle,
      fallbackMime: fallbackMime,
      resolvePlayback: () async => playback,
    );
  }

  Future<bool> _tryShareFromGallery({
    required String mediaId,
    required String mimeType,
    required String text,
  }) async {
    if (kIsWeb || defaultTargetPlatform != TargetPlatform.android) {
      return false;
    }
    final token = galleryMediaIdToken(mediaId);
    if (token.isEmpty) return false;

    try {
      final custom = _shareGalleryByMediaId;
      if (custom != null) {
        return custom(mediaIdToken: token, mimeType: mimeType, text: text);
      }
      final shared = await _channel.invokeMethod<bool>(
        _methodShareGalleryByMediaId,
        <String, Object?>{
          'mediaIdToken': token,
          'mimeType': mimeType,
          'text': text,
        },
      );
      return shared == true;
    } on PlatformException catch (error) {
      debugPrint(
        'SHARE: Gallery MediaStore lookup failed: ${error.code} ${error.message}',
      );
      return false;
    } catch (error) {
      debugPrint('SHARE: Gallery MediaStore lookup failed: $error');
      return false;
    }
  }

  Future<String?> _cacheDirPath() async {
    final custom = _resolveCacheDir;
    if (custom != null) {
      return custom();
    }
    return _channel.invokeMethod<String>(_methodGetCacheDir);
  }

  Future<void> _invokeShareFile({
    required String path,
    required String mimeType,
    required String text,
  }) async {
    try {
      final customShare = _shareFile;
      final shared = customShare != null
          ? await customShare(path: path, mimeType: mimeType, text: text)
          : await _channel.invokeMethod<bool>(_methodShareFile, {
              'path': path,
              'mimeType': mimeType,
              'text': text,
            });

      if (shared != true) {
        throw const ShareDownloadException('Native share sheet did not open.');
      }
    } on PlatformException catch (error) {
      throw ShareDownloadException(error.message ?? 'Share failed.');
    }
  }

  /// Streams HTTP response to [destination] via `srs-share-$id.tmp`, then rename.
  Future<void> _streamToFile({
    required Uri uri,
    required File destination,
  }) async {
    if (await _isValidCacheFile(destination)) return;

    final tmp = File(
      p.join(
        p.dirname(destination.path),
        '${p.basenameWithoutExtension(destination.path)}.tmp',
      ),
    );
    try {
      if (await tmp.exists()) {
        await tmp.delete();
      }
    } catch (_) {}

    final request = http.Request('GET', uri);
    final response = await _http.send(request);
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ShareDownloadException(
        'Could not download media to share (${response.statusCode}).',
      );
    }

    final sink = tmp.openWrite();
    try {
      await response.stream.pipe(sink);
      await sink.flush();
      await sink.close();
    } catch (error) {
      try {
        await sink.close();
      } catch (_) {}
      try {
        if (await tmp.exists()) {
          await tmp.delete();
        }
      } catch (_) {}
      rethrow;
    }

    final length = await tmp.length();
    if (length <= 0) {
      try {
        await tmp.delete();
      } catch (_) {}
      throw const ShareDownloadException('Downloaded file is empty.');
    }

    try {
      if (await destination.exists()) {
        await destination.delete();
      }
    } catch (_) {}
    await tmp.rename(destination.path);
  }

  String _mimeForPath(String path, String fallbackMime) {
    final lower = path.toLowerCase();
    if (lower.endsWith('.mov')) return 'video/quicktime';
    if (lower.endsWith('.jpg') || lower.endsWith('.jpeg')) return 'image/jpeg';
    if (lower.endsWith('.png')) return 'image/png';
    if (lower.endsWith('.webp')) return 'image/webp';
    if (lower.endsWith('.mp4')) return 'video/mp4';
    final mime = fallbackMime.split(';').first.trim();
    return mime.isEmpty ? 'video/mp4' : mime;
  }
}

class MissingPlaybackUrlException implements Exception {
  const MissingPlaybackUrlException();
}

class ShareDownloadException implements Exception {
  const ShareDownloadException(this.message);
  final String message;
}
