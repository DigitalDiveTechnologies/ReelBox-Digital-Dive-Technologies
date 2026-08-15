import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_client_provider.dart';
import '../../data/datasources/notification_local_store.dart';
import '../../data/datasources/notification_remote_datasource.dart';
import '../../data/repositories/notification_repository_impl.dart';
import '../../domain/models/notification_item.dart';
import '../../domain/repositories/notification_repository.dart';

final notificationRemoteDataSourceProvider =
    Provider<NotificationRemoteDataSource>((ref) {
      return NotificationRemoteDataSourceImpl(ref.watch(apiClientProvider));
    });

final notificationRepositoryProvider = Provider<NotificationRepository>((ref) {
  return NotificationRepositoryImpl(
    ref.watch(notificationRemoteDataSourceProvider),
  );
});

final notificationLocalStoreProvider = Provider<NotificationLocalStore>((ref) {
  return NotificationLocalStore();
});

/// Bumped after local read / delete so filtered providers rebuild.
final notificationLocalRevisionProvider = StateProvider<int>((ref) => 0);

final notificationsListProvider =
    FutureProvider.autoDispose<List<NotificationItem>>((ref) async {
      final repo = ref.watch(notificationRepositoryProvider);
      return repo.listNotifications(page: 1, pageSize: 50);
    });

/// API list with locally deleted IDs removed.
final filteredNotificationsProvider =
    FutureProvider.autoDispose<List<NotificationItem>>((ref) async {
      ref.watch(notificationLocalRevisionProvider);
      final items = await ref.watch(notificationsListProvider.future);
      final deletedIds = await ref
          .read(notificationLocalStoreProvider)
          .getDeletedIds();
      return items
          .where((item) => !deletedIds.contains(item.id))
          .toList(growable: false);
    });

/// Unread = listed items not in local read IDs and not in local deleted IDs.
/// Backend `isRead` is ignored for the Home badge.
final unreadNotificationCountProvider = Provider<int>((ref) {
  ref.watch(notificationLocalRevisionProvider);
  final items = ref.watch(notificationsListProvider).asData?.value;
  if (items == null) return 0;

  final local = ref.watch(_notificationLocalIdsProvider).asData?.value;
  if (local == null) return 0;

  return items
      .where(
        (item) =>
            !local.readIds.contains(item.id) &&
            !local.deletedIds.contains(item.id),
      )
      .length;
});

final _notificationLocalIdsProvider =
    FutureProvider<({Set<String> readIds, Set<String> deletedIds})>((
      ref,
    ) async {
      ref.watch(notificationLocalRevisionProvider);
      final store = ref.read(notificationLocalStoreProvider);
      return (
        readIds: await store.getReadIds(),
        deletedIds: await store.getDeletedIds(),
      );
    });

Future<void> markAllNotificationsAsRead(WidgetRef ref) async {
  final items = await ref.read(filteredNotificationsProvider.future);
  if (items.isEmpty) return;

  final store = ref.read(notificationLocalStoreProvider);
  final alreadyRead = await store.getReadIds();
  final idsToMark = items
      .map((item) => item.id.trim())
      .where((id) => id.isNotEmpty && !alreadyRead.contains(id));
  if (idsToMark.isEmpty) return;

  await store.markAllAsRead(idsToMark);
  ref.read(notificationLocalRevisionProvider.notifier).state++;
}

Future<void> deleteNotification(WidgetRef ref, String id) async {
  final trimmed = id.trim();
  if (trimmed.isEmpty) return;

  await ref.read(notificationLocalStoreProvider).markAsDeleted(trimmed);
  ref.read(notificationLocalRevisionProvider.notifier).state++;
}
