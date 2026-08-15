import 'package:shared_preferences/shared_preferences.dart';

/// Local read / deleted tracking for in-app notifications.
///
/// The API remains the source of the full list. This store only hides deleted
/// IDs and treats local read IDs as the unread-badge source of truth.
class NotificationLocalStore {
  NotificationLocalStore({SharedPreferences? preferences})
    // ignore: prefer_initializing_formals
    : _preferences = preferences;

  static const String readIdsKey = 'read_notification_ids_v1';
  static const String deletedIdsKey = 'deleted_notification_ids_v1';

  SharedPreferences? _preferences;
  Set<String>? _readMemory;
  Set<String>? _deletedMemory;

  Future<SharedPreferences> _prefs() async {
    return _preferences ??= await SharedPreferences.getInstance();
  }

  Future<Set<String>> _loaded({
    required String key,
    required Set<String>? memory,
    required void Function(Set<String> value) cache,
  }) async {
    if (memory != null) return memory;
    final prefs = await _prefs();
    final loaded = (prefs.getStringList(key) ?? const <String>[]).toSet();
    cache(loaded);
    return loaded;
  }

  Future<Set<String>> getReadIds() async {
    final set = await _loaded(
      key: readIdsKey,
      memory: _readMemory,
      cache: (value) => _readMemory = value,
    );
    return Set<String>.from(set);
  }

  Future<Set<String>> getDeletedIds() async {
    final set = await _loaded(
      key: deletedIdsKey,
      memory: _deletedMemory,
      cache: (value) => _deletedMemory = value,
    );
    return Set<String>.from(set);
  }

  Future<void> markAsRead(String id) async {
    await markAllAsRead([id]);
  }

  Future<void> markAllAsRead(Iterable<String> ids) async {
    final set = await _loaded(
      key: readIdsKey,
      memory: _readMemory,
      cache: (value) => _readMemory = value,
    );
    var changed = false;
    for (final raw in ids) {
      final id = raw.trim();
      if (id.isEmpty) continue;
      if (set.add(id)) changed = true;
    }
    if (!changed) return;
    _readMemory = set;
    final prefs = await _prefs();
    await prefs.setStringList(readIdsKey, set.toList(growable: false));
  }

  Future<void> markAsDeleted(String id) async {
    final trimmed = id.trim();
    if (trimmed.isEmpty) return;
    final set = await _loaded(
      key: deletedIdsKey,
      memory: _deletedMemory,
      cache: (value) => _deletedMemory = value,
    );
    if (!set.add(trimmed)) return;
    _deletedMemory = set;
    final prefs = await _prefs();
    await prefs.setStringList(deletedIdsKey, set.toList(growable: false));
  }
}
