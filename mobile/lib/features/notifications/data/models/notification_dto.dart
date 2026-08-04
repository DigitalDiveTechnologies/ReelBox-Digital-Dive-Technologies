import '../../domain/models/notification_item.dart';

class NotificationDto {
  const NotificationDto({
    required this.id,
    required this.title,
    required this.message,
    required this.createdAt,
    required this.isRead,
    this.mediaId,
  });

  final String id;
  final String title;
  final String message;
  final DateTime createdAt;
  final bool isRead;
  final String? mediaId;

  factory NotificationDto.fromJson(Map<String, dynamic> json) {
    return NotificationDto(
      id: json['id']?.toString() ?? '',
      title: json['title']?.toString() ?? '',
      message: json['message']?.toString() ?? '',
      createdAt: DateTime.tryParse(json['createdAt']?.toString() ?? '') ??
          DateTime.now().toUtc(),
      isRead: json['isRead'] == true,
      mediaId: json['mediaId']?.toString(),
    );
  }

  NotificationItem toItem() => NotificationItem(
        id: id,
        title: title,
        body: message,
        createdAt: createdAt.isUtc ? createdAt : createdAt.toUtc(),
        isRead: isRead,
      );
}

class NotificationListDto {
  const NotificationListDto({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.totalPages,
  });

  final List<NotificationDto> items;
  final int page;
  final int pageSize;
  final int totalCount;
  final int totalPages;

  factory NotificationListDto.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'];
    final items = rawItems is List
        ? rawItems
            .whereType<Map>()
            .map((e) => NotificationDto.fromJson(Map<String, dynamic>.from(e)))
            .toList(growable: false)
        : const <NotificationDto>[];

    return NotificationListDto(
      items: items,
      page: (json['page'] as num?)?.toInt() ?? 1,
      pageSize: (json['pageSize'] as num?)?.toInt() ?? 20,
      totalCount: (json['totalCount'] as num?)?.toInt() ?? items.length,
      totalPages: (json['totalPages'] as num?)?.toInt() ?? 0,
    );
  }
}
