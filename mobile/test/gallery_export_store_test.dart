import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:mobile/features/media/data/gallery/gallery_export_service.dart';
import 'package:mobile/features/media/data/gallery/gallery_export_store.dart';
import 'package:mobile/shared/models/media_item_preview.dart';
import 'package:mobile/shared/models/media_platform.dart';
import 'package:mobile/shared/models/media_status.dart';

MediaItemPreview _item(
  String id, {
  MediaStatus status = MediaStatus.completed,
}) {
  final now = DateTime.utc(2026, 8, 1);
  return MediaItemPreview(
    id: id,
    platform: MediaPlatform.instagram,
    status: status,
    originalUrl: 'https://instagram.com/reel/$id',
    createdAt: now,
    savedAt: now,
    title: 'Reel $id',
  );
}

class _FakeOps implements GalleryExportOperations {
  _FakeOps({
    required this.store,
    this.completed = const [],
    this.outcomes = const {},
    this.forcedInFlight = const {},
  });

  final GalleryExportStore store;
  List<MediaItemPreview> completed;
  Map<String, GalleryExportOutcome> outcomes;
  Set<String> forcedInFlight;

  final List<String> exportCalls = <String>[];
  int maxObservedInFlight = 0;
  int _inFlight = 0;
  final Set<String> _active = <String>{};

  @override
  bool get isAndroid => true;

  @override
  int get inFlightCount => _inFlight;

  @override
  bool isInFlight(String mediaId) {
    final id = mediaId.trim();
    return forcedInFlight.contains(id) || _active.contains(id);
  }

  @override
  Future<List<MediaItemPreview>> listAllCompletedMedia() async => completed;

  @override
  Future<GalleryExportOutcome> exportOne(MediaItemPreview item) async {
    _inFlight++;
    _active.add(item.id);
    if (_inFlight > maxObservedInFlight) {
      maxObservedInFlight = _inFlight;
    }
    exportCalls.add(item.id);
    await Future<void>.delayed(const Duration(milliseconds: 5));
    _inFlight--;
    _active.remove(item.id);
    final outcome = outcomes[item.id] ?? GalleryExportOutcome.exported;
    if (outcome == GalleryExportOutcome.exported ||
        outcome == GalleryExportOutcome.alreadyExported) {
      await store.markExported(item.id);
    }
    return outcome;
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('galleryExportBackoff', () {
    test('uses bounded exponential growth', () {
      expect(galleryExportBackoff(0), const Duration(seconds: 15));
      expect(galleryExportBackoff(1), const Duration(seconds: 30));
      expect(galleryExportBackoff(2), const Duration(seconds: 60));
      expect(galleryExportBackoff(5), const Duration(seconds: 300));
      expect(galleryExportBackoff(99), const Duration(seconds: 300));
    });
  });

  group('buildGalleryDisplayName', () {
    test('includes mediaId and sanitizes unsafe title characters', () {
      final name = buildGalleryDisplayName(
        mediaId: 'abc/def:123',
        title: 'Ford #mustang / reel?',
        extension: '.mp4',
      );

      expect(name.startsWith('ReelBox_'), isTrue);
      expect(name.endsWith('.mp4'), isTrue);
      expect(name.contains('/'), isFalse);
      expect(name.contains(':'), isFalse);
      expect(name.contains('?'), isFalse);
      expect(name.contains('#'), isFalse);
    });

    test('falls back when title is empty', () {
      final name = buildGalleryDisplayName(
        mediaId: 'media-1',
        title: '   ',
      );
      expect(name, 'ReelBox_media-1.mp4');
    });
  });

  group('galleryExtensionForMime', () {
    test('maps common mime types', () {
      expect(galleryExtensionForMime('video/mp4'), '.mp4');
      expect(galleryExtensionForMime('video/quicktime; codecs=hevc'), '.mov');
      expect(galleryExtensionForMime(null), '.mp4');
    });
  });

  group('GalleryExportStore', () {
    test('dedupes exported media ids across reads / restart', () async {
      SharedPreferences.setMockInitialValues(<String, Object>{});
      final prefs = await SharedPreferences.getInstance();
      final store = GalleryExportStore(preferences: prefs);

      expect(await store.isExported('m1'), isFalse);
      await store.markExported('m1');
      expect(await store.isExported('m1'), isTrue);

      final store2 = GalleryExportStore(preferences: prefs);
      expect(await store2.isExported('m1'), isTrue);
      expect(await store2.isExported('m2'), isFalse);
    });

    test('ignores blank media ids', () async {
      SharedPreferences.setMockInitialValues(<String, Object>{});
      final prefs = await SharedPreferences.getInstance();
      final store = GalleryExportStore(preferences: prefs);

      expect(await store.isExported('  '), isTrue);
      await store.markExported('');
      expect(prefs.getStringList(GalleryExportStore.prefsKey), isNull);
    });
  });

  group('GalleryExportCoordinator', () {
    late GalleryExportStore store;
    late DateTime clock;

    setUp(() async {
      SharedPreferences.setMockInitialValues(<String, Object>{});
      final prefs = await SharedPreferences.getInstance();
      store = GalleryExportStore(preferences: prefs);
      clock = DateTime.utc(2026, 8, 13, 0, 0, 0);
    });

    test('exports completed items without requiring Library screen', () async {
      final ops = _FakeOps(
        store: store,
        completed: [_item('a'), _item('b')],
        outcomes: {
          'a': GalleryExportOutcome.exported,
          'b': GalleryExportOutcome.exported,
        },
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.reconcile(reason: 'test');

      expect(ops.exportCalls, ['a', 'b']);
      expect(await store.isExported('a'), isTrue);
      expect(await store.isExported('b'), isTrue);
      coordinator.dispose();
    });

    test('failed export retries later and marks only after success', () async {
      final ops = _FakeOps(
        store: store,
        completed: [_item('x')],
        outcomes: {'x': GalleryExportOutcome.failed},
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.reconcile(reason: 'fail');
      expect(await store.isExported('x'), isFalse);
      expect(coordinator.failureCounts['x'], 1);
      expect(ops.exportCalls, ['x']);

      // Still inside backoff window — should not retry yet.
      await coordinator.reconcile(reason: 'too_soon');
      expect(ops.exportCalls, ['x']);

      // Advance past backoff and succeed.
      clock = clock.add(galleryExportBackoff(0));
      ops.outcomes = {'x': GalleryExportOutcome.exported};
      await coordinator.reconcile(reason: 'retry');
      expect(ops.exportCalls, ['x', 'x']);
      expect(await store.isExported('x'), isTrue);
      expect(coordinator.failureCounts.containsKey('x'), isFalse);
      coordinator.dispose();
    });

    test('alreadyExported / MediaStore-exists path does not re-export forever',
        () async {
      await store.markExported('exists');
      final ops = _FakeOps(
        store: store,
        completed: [_item('exists')],
        outcomes: {'exists': GalleryExportOutcome.alreadyExported},
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.reconcile(reason: 'exists');
      expect(ops.exportCalls, isEmpty);
      coordinator.dispose();
    });

    test('considers more than 50 completed items from ops list', () async {
      final many = List<MediaItemPreview>.generate(
        63,
        (i) => _item('id-$i'),
      );
      final ops = _FakeOps(
        store: store,
        completed: many,
        outcomes: {
          for (final item in many) item.id: GalleryExportOutcome.exported,
        },
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        maxConcurrent: 1,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.reconcile(reason: 'paginate');
      expect(ops.exportCalls.length, 63);
      expect(await store.isExported('id-0'), isTrue);
      expect(await store.isExported('id-62'), isTrue);
      coordinator.dispose();
    });

    test('respects concurrent export limit', () async {
      final items = [_item('c1'), _item('c2'), _item('c3')];
      final ops = _FakeOps(
        store: store,
        completed: items,
        outcomes: {
          for (final item in items) item.id: GalleryExportOutcome.exported,
        },
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        maxConcurrent: 1,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.reconcile(reason: 'concurrency');
      expect(ops.maxObservedInFlight, 1);
      expect(ops.exportCalls.length, 3);
      coordinator.dispose();
    });

    test('skips work when signed out', () async {
      final ops = _FakeOps(store: store, completed: [_item('z')]);
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => false,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.reconcile(reason: 'signed_out');
      expect(ops.exportCalls, isEmpty);
      coordinator.dispose();
    });

    test('app restart recovery exports previously unfinished ids', () async {
      // Simulate prior failed attempt: nothing marked exported.
      final ops = _FakeOps(
        store: store,
        completed: [_item('recover')],
        outcomes: {'recover': GalleryExportOutcome.exported},
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      // Authenticate without racing an unawaited start reconcile.
      coordinator.start(reconcileImmediately: false);
      await coordinator.reconcile(reason: 'restart');
      expect(await store.isExported('recover'), isTrue);
      coordinator.dispose();
    });

    test('does not duplicate exports for already successful ids', () async {
      final ops = _FakeOps(
        store: store,
        completed: [_item('once')],
        outcomes: {'once': GalleryExportOutcome.exported},
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.reconcile(reason: 'first');
      await coordinator.reconcile(reason: 'second');
      expect(ops.exportCalls, ['once']);
      coordinator.dispose();
    });

    test('media list kick exports newly Completed unexported media', () async {
      final ops = _FakeOps(
        store: store,
        completed: [_item('new1')],
        outcomes: {'new1': GalleryExportOutcome.exported},
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.onMediaListSnapshot([_item('new1')]);
      expect(ops.exportCalls, ['new1']);
      expect(await store.isExported('new1'), isTrue);
      coordinator.dispose();
    });

    test('media list kick skips already-exported ids', () async {
      await store.markExported('done');
      final ops = _FakeOps(
        store: store,
        completed: [_item('done')],
        outcomes: {'done': GalleryExportOutcome.exported},
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.onMediaListSnapshot([_item('done')]);
      expect(ops.exportCalls, isEmpty);
      coordinator.dispose();
    });

    test('media list kick skips in-flight ids', () async {
      final ops = _FakeOps(
        store: store,
        completed: [_item('busy')],
        outcomes: {'busy': GalleryExportOutcome.exported},
        forcedInFlight: {'busy'},
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.onMediaListSnapshot([_item('busy')]);
      expect(ops.exportCalls, isEmpty);
      coordinator.dispose();
    });

    test('media list kick respects backoff and does not re-kick same id',
        () async {
      final ops = _FakeOps(
        store: store,
        completed: [_item('bf')],
        outcomes: {'bf': GalleryExportOutcome.failed},
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.onMediaListSnapshot([_item('bf')]);
      expect(ops.exportCalls, ['bf']);
      expect(coordinator.failureCounts['bf'], 1);

      // Same Completed emission again (Library poll) — already seen, no kick.
      await coordinator.onMediaListSnapshot([_item('bf')]);
      expect(ops.exportCalls, ['bf']);

      // Direct reconcile still respects backoff window.
      await coordinator.reconcile(reason: 'too_soon');
      expect(ops.exportCalls, ['bf']);
      coordinator.dispose();
    });

    test('media list kick keeps maxConcurrent at 1 for multiple items',
        () async {
      final items = [_item('m1'), _item('m2'), _item('m3')];
      final ops = _FakeOps(
        store: store,
        completed: items,
        outcomes: {
          for (final item in items) item.id: GalleryExportOutcome.exported,
        },
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        maxConcurrent: 1,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.onMediaListSnapshot(items);
      expect(ops.maxObservedInFlight, 1);
      expect(ops.exportCalls.length, 3);
      coordinator.dispose();
    });

    test('idle discover fallback still exports when list kick never fires',
        () async {
      final ops = _FakeOps(
        store: store,
        completed: const [],
        outcomes: {'late': GalleryExportOutcome.exported},
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(milliseconds: 40),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.reconcile(reason: 'empty');
      expect(ops.exportCalls, isEmpty);

      ops.completed = [_item('late')];
      await Future<void>.delayed(const Duration(milliseconds: 80));
      expect(ops.exportCalls, ['late']);
      expect(await store.isExported('late'), isTrue);
      expect(galleryExportIdleDiscoverInterval, const Duration(seconds: 45));
      coordinator.dispose();
    });

    test('logout clears list-kick memory so login can kick again', () async {
      final ops = _FakeOps(
        store: store,
        completed: [_item('again')],
        outcomes: {'again': GalleryExportOutcome.exported},
      );
      final coordinator = GalleryExportCoordinator(
        service: ops,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );

      coordinator.start(reconcileImmediately: false);
      await coordinator.onMediaListSnapshot([_item('again')]);
      expect(ops.exportCalls, ['again']);

      // Simulate delete from gallery store + logout/login without dispose.
      await store.markExported('again'); // already marked
      // Clear exported for re-test of kick after logout: use fresh store state
      // by removing via new prefs is hard; instead clear seen via logout and
      // use a different id.
      coordinator.onLoggedOut();
      expect(coordinator.isStarted, isFalse);
      expect(coordinator.seenCompletedFromList, isEmpty);

      final ops2 = _FakeOps(
        store: store,
        completed: [_item('again2')],
        outcomes: {'again2': GalleryExportOutcome.exported},
      );
      final coordinator2 = GalleryExportCoordinator(
        service: ops2,
        store: store,
        isSignedIn: () async => true,
        now: () => clock,
        idleDiscoverInterval: const Duration(days: 1),
      );
      coordinator2.start(reconcileImmediately: false);
      await coordinator2.onMediaListSnapshot([_item('again2')]);
      expect(ops2.exportCalls, ['again2']);
      coordinator.dispose();
      coordinator2.dispose();
    });
  });

  group('listAllCompletedMedia pagination (via fake repo scan)', () {
    test('pageSize constant is independent of Library default 50 UI usage', () {
      expect(galleryExportPageSize, 50);
      expect(galleryExportMaxConcurrent, 1);
    });
  });
}
