import '../../../../core/constants/api_endpoints.dart';
import '../../../../core/network/api_client.dart';
import '../models/notification_dto.dart';

abstract class NotificationRemoteDataSource {
  Future<NotificationListDto> listNotifications({
    int page = 1,
    int pageSize = 50,
  });
}

class NotificationRemoteDataSourceImpl implements NotificationRemoteDataSource {
  NotificationRemoteDataSourceImpl(this._api);

  final ApiClient _api;

  @override
  Future<NotificationListDto> listNotifications({
    int page = 1,
    int pageSize = 50,
  }) async {
    final json = await _api.getJson(
      ApiEndpoints.notifications,
      queryParameters: {
        'page': '$page',
        'pageSize': '$pageSize',
      },
    );
    return NotificationListDto.fromJson(json);
  }
}
