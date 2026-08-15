import '../../../shared/models/media_item_preview.dart';
import '../../../shared/models/media_status.dart';

/// Same-platform then other-platform related groups (Task 2).
///
/// Each list is `completed` only, excludes [current], sorted `createdAt` DESC.
/// Layout must render [samePlatform] rows to completion before [otherPlatform]
/// so the next platform never shares the last row.
({
  List<MediaItemPreview> samePlatform,
  List<MediaItemPreview> otherPlatform,
}) buildRelatedReelGroups({
  required MediaItemPreview current,
  required List<MediaItemPreview> all,
}) {
  final currentId = current.id.trim();
  final completed = <MediaItemPreview>[];
  for (final item in all) {
    if (item.status != MediaStatus.completed) continue;
    if (item.id.trim().isEmpty || item.id.trim() == currentId) continue;
    completed.add(item);
  }

  final same = completed
      .where((m) => m.platform == current.platform)
      .toList(growable: true)
    ..sort((a, b) => b.createdAt.compareTo(a.createdAt));

  final other = completed
      .where((m) => m.platform != current.platform)
      .toList(growable: true)
    ..sort((a, b) => b.createdAt.compareTo(a.createdAt));

  return (
    samePlatform: List<MediaItemPreview>.unmodifiable(same),
    otherPlatform: List<MediaItemPreview>.unmodifiable(other),
  );
}

/// Flat related list: same platform first, then other (ordering tests).
List<MediaItemPreview> buildRelatedReels({
  required MediaItemPreview current,
  required List<MediaItemPreview> all,
}) {
  final groups = buildRelatedReelGroups(current: current, all: all);
  return <MediaItemPreview>[
    ...groups.samePlatform,
    ...groups.otherPlatform,
  ];
}

/// Vertical player feed.
///
/// Same-platform completed reels keep their `createdAt` DESC order. The opened
/// reel is always first (page 0). Items before it in that order come next,
/// then items after it, then the other platform (never interleaved).
///
/// Opened B in A-B-C-D → B, A, C, D.
List<MediaItemPreview> buildVerticalReelFeed({
  required MediaItemPreview current,
  required List<MediaItemPreview> all,
}) {
  if (current.status != MediaStatus.completed) {
    return <MediaItemPreview>[current];
  }

  final currentId = current.id.trim();
  final same = all
      .where(
        (m) =>
            m.status == MediaStatus.completed &&
            m.platform == current.platform &&
            m.id.trim().isNotEmpty,
      )
      .toList(growable: true)
    ..sort((a, b) => b.createdAt.compareTo(a.createdAt));

  final currentIndex = same.indexWhere((m) => m.id.trim() == currentId);
  if (currentIndex < 0) {
    same.insert(0, current);
  }

  final index = same.indexWhere((m) => m.id.trim() == currentId);
  final before = same.sublist(0, index);
  final after = same.sublist(index + 1);

  final other = buildRelatedReelGroups(current: current, all: all).otherPlatform;

  return <MediaItemPreview>[
    same[index],
    ...before,
    ...after,
    ...other,
  ];
}
