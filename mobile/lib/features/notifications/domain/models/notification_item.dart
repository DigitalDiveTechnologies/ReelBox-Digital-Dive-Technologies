/// In-app notification item for list UI / repository mapping.
class NotificationItem {
  const NotificationItem({
    required this.id,
    required this.title,
    required this.body,
    required this.createdAt,
    this.isRead = false,
    this.mediaId,
  });

  final String id;
  final String title;
  final String body;
  final DateTime createdAt;
  final bool isRead;

  /// Linked media when the API includes a non-empty `mediaId`.
  final String? mediaId;
}
