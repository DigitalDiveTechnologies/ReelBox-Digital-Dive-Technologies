import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:http/http.dart' as http;

import '../../../core/network/media_url_resolver.dart';
import '../../media/data/models/media_dto.dart';

/// Downloads completed media to app cache and opens the native Android share sheet.
class MediaShareService {
  MediaShareService({
    MethodChannel? channel,
    http.Client? httpClient,
  })  : _channel = channel ?? const MethodChannel(_channelName),
        _http = httpClient ?? http.Client();

  static const String _channelName = 'com.example.mobile/share_intent';
  static const String _methodGetCacheDir = 'getCacheDir';
  static const String _methodShareFile = 'shareFile';

  final MethodChannel _channel;
  final http.Client _http;

  Future<void> shareDownloadedFile({
    required String mediaId,
    required String displayTitle,
    required PlaybackDto playback,
    required String fallbackMime,
  }) async {
    if (kIsWeb) {
      throw const ShareDownloadException('Sharing is not supported on web.');
    }

    final rawUrl = playback.playbackUrl?.trim();
    if (rawUrl == null || rawUrl.isEmpty) {
      throw const MissingPlaybackUrlException();
    }

    final uri = resolveSignedMediaUrl(rawUrl);
    final response = await _http.get(uri);
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ShareDownloadException(
        'Could not download media to share (${response.statusCode}).',
      );
    }
    if (response.bodyBytes.isEmpty) {
      throw const ShareDownloadException('Downloaded file is empty.');
    }

    final mime = (playback.mimeType ?? fallbackMime).split(';').first.trim();
    final extension = switch (mime) {
      'video/quicktime' => '.mov',
      'image/jpeg' => '.jpg',
      'image/png' => '.png',
      'image/webp' => '.webp',
      _ => '.mp4',
    };

    final cacheDir = await _channel.invokeMethod<String>(_methodGetCacheDir);
    if (cacheDir == null || cacheDir.isEmpty) {
      throw const ShareDownloadException('Could not access app cache for sharing.');
    }

    final filePath = '$cacheDir/srs-share-$mediaId$extension';
    await File(filePath).writeAsBytes(response.bodyBytes, flush: true);

    try {
      final shared = await _channel.invokeMethod<bool>(_methodShareFile, {
        'path': filePath,
        'mimeType': mime,
        'text': displayTitle,
      });

      if (shared != true) {
        throw const ShareDownloadException('Native share sheet did not open.');
      }
    } on PlatformException catch (error) {
      throw ShareDownloadException(error.message ?? 'Share failed.');
    }
  }
}

class MissingPlaybackUrlException implements Exception {
  const MissingPlaybackUrlException();
}

class ShareDownloadException implements Exception {
  const ShareDownloadException(this.message);
  final String message;
}
