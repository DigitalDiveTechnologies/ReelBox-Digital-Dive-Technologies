import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:path/path.dart' as p;

import 'package:mobile/features/media/data/models/media_dto.dart';
import 'package:mobile/features/media_detail/data/media_share_service.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  late Directory tempDir;
  late List<Map<String, String>> shareCalls;
  late List<Map<String, String>> galleryShareCalls;
  late int playbackResolves;

  setUp(() async {
    tempDir = await Directory.systemTemp.createTemp('reelbox_share_');
    shareCalls = <Map<String, String>>[];
    galleryShareCalls = <Map<String, String>>[];
    playbackResolves = 0;
  });

  tearDown(() async {
    if (await tempDir.exists()) {
      await tempDir.delete(recursive: true);
    }
  });

  MediaShareService buildService({
    http.Client? client,
    Future<bool> Function({
      required String mediaIdToken,
      required String mimeType,
      required String text,
    })? shareGalleryByMediaId,
  }) {
    return MediaShareService(
      httpClient: client,
      resolveCacheDir: () async => tempDir.path,
      shareFile: ({
        required String path,
        required String mimeType,
        required String text,
      }) async {
        shareCalls.add({
          'path': path,
          'mimeType': mimeType,
          'text': text,
        });
        return true;
      },
      // Default: no Gallery hit (avoids MethodChannel noise in unit tests).
      shareGalleryByMediaId: shareGalleryByMediaId ??
          ({
            required String mediaIdToken,
            required String mimeType,
            required String text,
          }) async =>
              false,
    );
  }


  PlaybackDto playback({required String url}) {
    playbackResolves++;
    return PlaybackDto(
      mediaId: 'm1',
      status: 'completed',
      delivery: 'application_signed_url',
      playbackUrl: url,
      mimeType: 'video/mp4',
    );
  }

  test('reuses existing local share cache without network or playback', () async {
    final cached = File(
      p.join(tempDir.path, MediaShareService.shareCacheFileName('m1', '.mp4')),
    );
    await cached.writeAsBytes(utf8.encode('cached-video-bytes'));

    final service = buildService(
      client: MockClient((request) async {
        fail('Network should not be called when cache exists');
      }),
      shareGalleryByMediaId: ({
        required String mediaIdToken,
        required String mimeType,
        required String text,
      }) async {
        fail('Gallery should not be queried when share cache exists');
      },
    );

    await service.shareCompletedMedia(
      mediaId: 'm1',
      displayTitle: 'Reel',
      fallbackMime: 'video/mp4',
      resolvePlayback: () async {
        fail('Playback should not be resolved when cache exists');
      },
    );

    expect(playbackResolves, 0);
    expect(shareCalls, hasLength(1));
    expect(shareCalls.single['path'], cached.path);
    expect(shareCalls.single['mimeType'], 'video/mp4');
  });

  test(
    'first share reuses Gallery MediaStore without VPS download when cache missing',
    () async {
      debugDefaultTargetPlatformOverride = TargetPlatform.android;
      addTearDown(() {
        debugDefaultTargetPlatformOverride = null;
      });

      final service = buildService(
        client: MockClient((request) async {
          fail('Network should not be called when Gallery video exists');
        }),
        shareGalleryByMediaId: ({
          required String mediaIdToken,
          required String mimeType,
          required String text,
        }) async {
          galleryShareCalls.add({
            'mediaIdToken': mediaIdToken,
            'mimeType': mimeType,
            'text': text,
          });
          return true;
        },
      );

      await service.shareCompletedMedia(
        mediaId: 'gallery-reel-1',
        displayTitle: 'From Gallery',
        fallbackMime: 'video/mp4',
        resolvePlayback: () async {
          fail('Playback should not be resolved when Gallery share succeeds');
        },
      );

      expect(playbackResolves, 0);
      expect(shareCalls, isEmpty);
      expect(galleryShareCalls, hasLength(1));
      expect(
        galleryShareCalls.single['mediaIdToken'],
        MediaShareService.galleryMediaIdToken('gallery-reel-1'),
      );
      expect(galleryShareCalls.single['mimeType'], 'video/mp4');
      expect(galleryShareCalls.single['text'], 'From Gallery');
    },
  );

  test(
    'falls back to VPS stream when Gallery MediaStore lookup misses',
    () async {
      debugDefaultTargetPlatformOverride = TargetPlatform.android;
      addTearDown(() {
        debugDefaultTargetPlatformOverride = null;
      });

      final payload = utf8.encode('gallery-miss-then-download');
      final service = buildService(
        client: MockClient((request) async {
          return http.Response.bytes(payload, 200);
        }),
        shareGalleryByMediaId: ({
          required String mediaIdToken,
          required String mimeType,
          required String text,
        }) async {
          galleryShareCalls.add({'mediaIdToken': mediaIdToken});
          return false;
        },
      );

      await service.shareCompletedMedia(
        mediaId: 'm-miss',
        displayTitle: 'Miss',
        fallbackMime: 'video/mp4',
        resolvePlayback: () async => playback(
          url: 'http://cdn.example.com/api/v1/media/m-miss/content?sig=1',
        ),
      );

      expect(galleryShareCalls, hasLength(1));
      expect(galleryShareCalls.single['mediaIdToken'], isNotNull);
      expect(playbackResolves, 1);
      expect(shareCalls, hasLength(1));
      expect(await File(shareCalls.single['path']!).length(), payload.length);
    },
  );


  test('streams to cache then shares when cache is missing', () async {
    final payload = List<int>.generate(2048, (i) => i % 256);
    final service = buildService(
      client: MockClient((request) async {
        expect(request.method, 'GET');
        expect(request.url.path.contains('/api/v1/media/m2/content'), isTrue);
        return http.Response.bytes(payload, 200);
      }),
    );

    await service.shareCompletedMedia(
      mediaId: 'm2',
      displayTitle: 'Fresh',
      fallbackMime: 'video/mp4',
      resolvePlayback: () async => playback(
        url: 'http://cdn.example.com/api/v1/media/m2/content?sig=1',
      ),
    );

    expect(playbackResolves, 1);
    expect(shareCalls, hasLength(1));
    final sharedPath = shareCalls.single['path']!;
    expect(sharedPath.contains('srs-share-m2'), isTrue);
    final file = File(sharedPath);
    expect(await file.exists(), isTrue);
    expect(await file.length(), payload.length);
    expect(
      await File(
        p.join(tempDir.path, 'srs-share-m2.tmp'),
      ).exists(),
      isFalse,
    );
  });

  test('second share reuses the file created by the first share', () async {
    final payload = utf8.encode('once-only-download');
    var downloads = 0;
    final service = buildService(
      client: MockClient((request) async {
        downloads++;
        return http.Response.bytes(payload, 200);
      }),
    );

    Future<void> shareOnce() {
      return service.shareCompletedMedia(
        mediaId: 'm3',
        displayTitle: 'Again',
        fallbackMime: 'video/mp4',
        resolvePlayback: () async => playback(
          url: 'http://cdn.example.com/v.mp4',
        ),
      );
    }

    await shareOnce();
    await shareOnce();

    expect(downloads, 1);
    expect(playbackResolves, 1);
    expect(shareCalls, hasLength(2));
  });

  test('failed download does not leave a shareable incomplete file', () async {
    final service = buildService(
      client: MockClient((request) async {
        return http.Response('nope', 500);
      }),
    );

    await expectLater(
      () => service.shareCompletedMedia(
        mediaId: 'm4',
        displayTitle: 'Fail',
        fallbackMime: 'video/mp4',
        resolvePlayback: () async => playback(
          url: 'http://cdn.example.com/bad.mp4',
        ),
      ),
      throwsA(isA<ShareDownloadException>()),
    );

    final expected = File(
      p.join(tempDir.path, MediaShareService.shareCacheFileName('m4', '.mp4')),
    );
    expect(await expected.exists(), isFalse);
    expect(shareCalls, isEmpty);
  });

  test('empty download is rejected and not shared', () async {
    final service = buildService(
      client: MockClient((request) async {
        return http.Response.bytes(const [], 200);
      }),
    );

    await expectLater(
      () => service.shareCompletedMedia(
        mediaId: 'm5',
        displayTitle: 'Empty',
        fallbackMime: 'video/mp4',
        resolvePlayback: () async => playback(
          url: 'http://cdn.example.com/empty.mp4',
        ),
      ),
      throwsA(isA<ShareDownloadException>()),
    );
    expect(shareCalls, isEmpty);
  });

  test('shareCacheFileName and galleryMediaIdToken stay stable', () {
    expect(
      MediaShareService.shareCacheFileName('abc/def', '.mp4'),
      'srs-share-abc_def.mp4',
    );
    expect(MediaShareService.extensionForMime('video/quicktime'), '.mov');
    expect(
      MediaShareService.galleryMediaIdToken('abc-123'),
      isNot(contains('.')),
    );
    expect(
      MediaShareService.galleryMediaIdToken('abc-123'),
      startsWith('abc'),
    );
  });

  test('ignores partial .tmp cache and does not treat it as a hit', () async {
    await File(
      p.join(tempDir.path, 'srs-share-m1.tmp'),
    ).writeAsBytes(utf8.encode('partial-not-valid'));

    final payload = utf8.encode('final-video');
    final service = buildService(
      client: MockClient((request) async {
        return http.Response.bytes(payload, 200);
      }),
    );

    await service.shareCompletedMedia(
      mediaId: 'm1',
      displayTitle: 'Tmp',
      fallbackMime: 'video/mp4',
      resolvePlayback: () async => playback(
        url: 'http://cdn.example.com/v.mp4',
      ),
    );

    expect(playbackResolves, 1);
    expect(shareCalls, hasLength(1));
    expect(
      await File(
        p.join(tempDir.path, MediaShareService.shareCacheFileName('m1', '.mp4')),
      ).length(),
      payload.length,
    );
    expect(
      await File(p.join(tempDir.path, 'srs-share-m1.tmp')).exists(),
      isFalse,
    );
  });
}
