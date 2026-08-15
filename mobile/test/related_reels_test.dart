import 'package:flutter_test/flutter_test.dart';

import 'package:mobile/features/media_detail/domain/related_reels.dart';
import 'package:mobile/shared/models/media_item_preview.dart';
import 'package:mobile/shared/models/media_platform.dart';
import 'package:mobile/shared/models/media_status.dart';

MediaItemPreview _reel({
  required String id,
  required MediaPlatform platform,
  required DateTime createdAt,
  MediaStatus status = MediaStatus.completed,
}) {
  return MediaItemPreview(
    id: id,
    platform: platform,
    status: status,
    originalUrl: 'https://example.com/$id',
    createdAt: createdAt,
    savedAt: createdAt,
    title: id,
  );
}

void main() {
  group('buildRelatedReels / buildRelatedReelGroups', () {
    final t1 = DateTime.utc(2026, 8, 1);
    final t2 = DateTime.utc(2026, 8, 2);
    final t3 = DateTime.utc(2026, 8, 3);
    final t4 = DateTime.utc(2026, 8, 4);
    final t5 = DateTime.utc(2026, 8, 5);

    test('same platform first (excluding current), then other platform', () {
      final current = _reel(
        id: 'fb1',
        platform: MediaPlatform.facebook,
        createdAt: t5,
      );
      final all = [
        current,
        _reel(id: 'fb2', platform: MediaPlatform.facebook, createdAt: t4),
        _reel(id: 'fb3', platform: MediaPlatform.facebook, createdAt: t2),
        _reel(id: 'ig1', platform: MediaPlatform.instagram, createdAt: t3),
        _reel(id: 'ig2', platform: MediaPlatform.instagram, createdAt: t1),
        _reel(
          id: 'fb-downloading',
          platform: MediaPlatform.facebook,
          createdAt: t5,
          status: MediaStatus.downloading,
        ),
      ];

      final related = buildRelatedReels(current: current, all: all);

      expect(related.map((e) => e.id).toList(), ['fb2', 'fb3', 'ig1', 'ig2']);
      expect(related.any((e) => e.id == 'fb1'), isFalse);
      expect(related.any((e) => e.id == 'fb-downloading'), isFalse);
    });

    test('instagram current puts remaining IG first then FB', () {
      final current = _reel(
        id: 'ig3',
        platform: MediaPlatform.instagram,
        createdAt: t3,
      );
      final all = [
        current,
        _reel(id: 'ig1', platform: MediaPlatform.instagram, createdAt: t1),
        _reel(id: 'ig2', platform: MediaPlatform.instagram, createdAt: t2),
        _reel(id: 'fb1', platform: MediaPlatform.facebook, createdAt: t4),
        _reel(id: 'fb2', platform: MediaPlatform.facebook, createdAt: t5),
      ];

      final related = buildRelatedReels(current: current, all: all);

      expect(related.map((e) => e.id).toList(), ['ig2', 'ig1', 'fb2', 'fb1']);
    });

    test('does not category-filter — only completed + platform order', () {
      final current = _reel(
        id: 'fb1',
        platform: MediaPlatform.facebook,
        createdAt: t1,
      );
      final a =
          _reel(id: 'fb2', platform: MediaPlatform.facebook, createdAt: t2);
      final b =
          _reel(id: 'ig1', platform: MediaPlatform.instagram, createdAt: t3);
      final related = buildRelatedReels(current: current, all: [current, a, b]);
      expect(related.map((e) => e.id).toList(), ['fb2', 'ig1']);
    });

    test('groups keep platforms separated for row-boundary layout', () {
      // TEST 1: 5 IG remaining + 3 FB with current IG
      final current = _reel(
        id: 'ig0',
        platform: MediaPlatform.instagram,
        createdAt: t5,
      );
      final all = [
        current,
        for (var i = 1; i <= 5; i++)
          _reel(
            id: 'ig$i',
            platform: MediaPlatform.instagram,
            createdAt: DateTime.utc(2026, 8, 10 - i),
          ),
        for (var i = 1; i <= 3; i++)
          _reel(
            id: 'fb$i',
            platform: MediaPlatform.facebook,
            createdAt: DateTime.utc(2026, 8, 6 - i),
          ),
      ];

      final groups = buildRelatedReelGroups(current: current, all: all);
      expect(groups.samePlatform.map((e) => e.id).toList(),
          ['ig1', 'ig2', 'ig3', 'ig4', 'ig5']);
      expect(groups.otherPlatform.map((e) => e.id).toList(),
          ['fb1', 'fb2', 'fb3']);
      // Odd same-platform count → layout leaves empty second cell before FB.
      expect(groups.samePlatform.length.isOdd, isTrue);
    });

    test('TEST 4: 2 FB then 1 IG when current is Facebook', () {
      final current = _reel(
        id: 'fb0',
        platform: MediaPlatform.facebook,
        createdAt: t5,
      );
      final all = [
        current,
        _reel(id: 'fb1', platform: MediaPlatform.facebook, createdAt: t4),
        _reel(id: 'fb2', platform: MediaPlatform.facebook, createdAt: t3),
        _reel(id: 'ig1', platform: MediaPlatform.instagram, createdAt: t2),
      ];
      final groups = buildRelatedReelGroups(current: current, all: all);
      expect(groups.samePlatform.map((e) => e.id).toList(), ['fb1', 'fb2']);
      expect(groups.otherPlatform.map((e) => e.id).toList(), ['ig1']);
      expect(groups.otherPlatform.length.isOdd, isTrue);
    });
  });

  group('buildVerticalReelFeed', () {
    final t1 = DateTime.utc(2026, 8, 1);
    final t2 = DateTime.utc(2026, 8, 2);
    final t3 = DateTime.utc(2026, 8, 3);
    final t4 = DateTime.utc(2026, 8, 4);
    final t5 = DateTime.utc(2026, 8, 5);

    test('opened reel is always first: B → A → C → D', () {
      final a = _reel(id: 'A', platform: MediaPlatform.instagram, createdAt: t4);
      final b = _reel(id: 'B', platform: MediaPlatform.instagram, createdAt: t3);
      final c = _reel(id: 'C', platform: MediaPlatform.instagram, createdAt: t2);
      final d = _reel(id: 'D', platform: MediaPlatform.instagram, createdAt: t1);
      final feed = buildVerticalReelFeed(
        current: b,
        all: [a, b, c, d],
      );
      expect(feed.map((e) => e.id).toList(), ['B', 'A', 'C', 'D']);
    });

    test('opened first reel keeps original same-platform sequence', () {
      final a = _reel(id: 'A', platform: MediaPlatform.instagram, createdAt: t4);
      final b = _reel(id: 'B', platform: MediaPlatform.instagram, createdAt: t3);
      final c = _reel(id: 'C', platform: MediaPlatform.instagram, createdAt: t2);
      final feed = buildVerticalReelFeed(current: a, all: [a, b, c]);
      expect(feed.map((e) => e.id).toList(), ['A', 'B', 'C']);
    });

    test('opened last reel then remaining same-platform from the start', () {
      final a = _reel(id: 'A', platform: MediaPlatform.instagram, createdAt: t4);
      final b = _reel(id: 'B', platform: MediaPlatform.instagram, createdAt: t3);
      final c = _reel(id: 'C', platform: MediaPlatform.instagram, createdAt: t2);
      final feed = buildVerticalReelFeed(current: c, all: [a, b, c]);
      expect(feed.map((e) => e.id).toList(), ['C', 'A', 'B']);
    });

    test('Instagram opened: remaining IG then all Facebook', () {
      final current = _reel(
        id: 'ig-B',
        platform: MediaPlatform.instagram,
        createdAt: t3,
      );
      final all = [
        _reel(id: 'ig-A', platform: MediaPlatform.instagram, createdAt: t4),
        current,
        _reel(id: 'ig-C', platform: MediaPlatform.instagram, createdAt: t2),
        _reel(id: 'ig-D', platform: MediaPlatform.instagram, createdAt: t1),
        _reel(id: 'fb-A', platform: MediaPlatform.facebook, createdAt: t5),
        _reel(id: 'fb-B', platform: MediaPlatform.facebook, createdAt: t3),
        _reel(id: 'fb-C', platform: MediaPlatform.facebook, createdAt: t1),
      ];

      final feed = buildVerticalReelFeed(current: current, all: all);
      expect(
        feed.map((e) => e.id).toList(),
        ['ig-B', 'ig-A', 'ig-C', 'ig-D', 'fb-A', 'fb-B', 'fb-C'],
      );
    });

    test('Facebook opened: remaining FB then all Instagram', () {
      final current = _reel(
        id: 'fb-B',
        platform: MediaPlatform.facebook,
        createdAt: t4,
      );
      final all = [
        current,
        _reel(id: 'fb-A', platform: MediaPlatform.facebook, createdAt: t5),
        _reel(id: 'fb-C', platform: MediaPlatform.facebook, createdAt: t2),
        _reel(id: 'ig-A', platform: MediaPlatform.instagram, createdAt: t3),
        _reel(id: 'ig-B', platform: MediaPlatform.instagram, createdAt: t1),
      ];

      final feed = buildVerticalReelFeed(current: current, all: all);
      expect(
        feed.map((e) => e.id).toList(),
        ['fb-B', 'fb-A', 'fb-C', 'ig-A', 'ig-B'],
      );
    });

    test('does not interleave platforms', () {
      final current = _reel(
        id: 'ig1',
        platform: MediaPlatform.instagram,
        createdAt: t1,
      );
      final feed = buildVerticalReelFeed(
        current: current,
        all: [
          current,
          _reel(id: 'fb1', platform: MediaPlatform.facebook, createdAt: t5),
          _reel(id: 'ig2', platform: MediaPlatform.instagram, createdAt: t3),
          _reel(id: 'fb2', platform: MediaPlatform.facebook, createdAt: t4),
        ],
      );
      expect(feed.map((e) => e.id).toList(), ['ig1', 'ig2', 'fb1', 'fb2']);
      final igEnd = feed.lastIndexWhere(
        (e) => e.platform == MediaPlatform.instagram,
      );
      final fbStart = feed.indexWhere(
        (e) => e.platform == MediaPlatform.facebook,
      );
      expect(igEnd, lessThan(fbStart));
    });
  });
}
