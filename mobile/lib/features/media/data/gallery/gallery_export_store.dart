import 'package:shared_preferences/shared_preferences.dart';

/// Persists which media IDs have already been exported to the device Gallery.
///
/// Prevents duplicate MediaStore copies when Library/Home poll every few seconds.
class GalleryExportStore {
  GalleryExportStore({SharedPreferences? preferences})
      // ignore: prefer_initializing_formals
      : _preferences = preferences;

  static const String prefsKey = 'gallery_exported_media_ids_v1';

  SharedPreferences? _preferences;
  Set<String>? _memory;

  Future<SharedPreferences> _prefs() async {
    return _preferences ??= await SharedPreferences.getInstance();
  }

  Future<Set<String>> _loaded() async {
    if (_memory != null) return _memory!;
    final prefs = await _prefs();
    final raw = prefs.getStringList(prefsKey) ?? const <String>[];
    _memory = raw.toSet();
    return _memory!;
  }

  Future<bool> isExported(String mediaId) async {
    final id = mediaId.trim();
    if (id.isEmpty) return true;
    final set = await _loaded();
    return set.contains(id);
  }

  Future<void> markExported(String mediaId) async {
    final id = mediaId.trim();
    if (id.isEmpty) return;
    final set = await _loaded();
    if (!set.add(id)) return;
    final prefs = await _prefs();
    await prefs.setStringList(prefsKey, set.toList(growable: false));
  }
}

/// Builds a MediaStore-safe display name for ReelBox gallery exports.
///
/// Always includes [mediaId] so retries / collisions stay unique per reel.
String buildGalleryDisplayName({
  required String mediaId,
  String? title,
  String extension = '.mp4',
}) {
  final ext = extension.startsWith('.') ? extension : '.$extension';
  final idPart = _sanitizeFileToken(mediaId, maxLength: 36);
  final titleRaw = (title ?? '').trim();
  final titlePart = titleRaw.isEmpty
      ? null
      : _sanitizeFileToken(titleRaw, maxLength: 40);

  final base = titlePart == null || titlePart.isEmpty
      ? 'ReelBox_$idPart'
      : 'ReelBox_${idPart}_$titlePart';
  return '$base$ext';
}

String _sanitizeFileToken(String raw, {required int maxLength}) {
  var cleaned = raw
      .replaceAll(RegExp(r'[^A-Za-z0-9._-]+'), '_')
      .replaceAll(RegExp(r'_+'), '_')
      .replaceAll(RegExp(r'^[._]+'), '')
      .replaceAll(RegExp(r'[._]+$'), '');
  if (cleaned.isEmpty) {
    cleaned = 'reel';
  }
  if (cleaned.length > maxLength) {
    cleaned = cleaned.substring(0, maxLength);
  }
  return cleaned;
}

String galleryExtensionForMime(String? mimeType) {
  final mime = (mimeType ?? 'video/mp4').split(';').first.trim().toLowerCase();
  return switch (mime) {
    'video/quicktime' => '.mov',
    'image/jpeg' => '.jpg',
    'image/png' => '.png',
    'image/webp' => '.webp',
    _ => '.mp4',
  };
}
