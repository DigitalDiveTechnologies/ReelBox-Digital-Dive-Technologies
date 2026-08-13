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
