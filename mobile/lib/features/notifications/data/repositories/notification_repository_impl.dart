import '../../domain/models/notification_item.dart';
import '../../domain/repositories/notification_repository.dart';
import '../datasources/notification_remote_datasource.dart';

class NotificationRepositoryImpl implements NotificationRepository {
  NotificationRepositoryImpl(this._remote);

  final NotificationRemoteDataSource _remote;

  @override
  Future<List<NotificationItem>> listNotifications({
    int page = 1,
    int pageSize = 50,
  }) async {
    final dto = await _remote.listNotifications(page: page, pageSize: pageSize);
    return dto.items.map((e) => e.toItem()).toList(growable: false);
  }
}
