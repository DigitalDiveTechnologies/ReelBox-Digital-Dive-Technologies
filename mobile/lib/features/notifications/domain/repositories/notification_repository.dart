import '../models/notification_item.dart';

abstract class NotificationRepository {
  Future<List<NotificationItem>> listNotifications({
    int page = 1,
    int pageSize = 50,
  });
}
