import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_constants.dart';
import '../../../../core/router/route_paths.dart';
import '../../../../shared/data/ui_placeholder_catalog.dart';
import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_status.dart';
import '../../../../shared/widgets/app_empty_state.dart';
import '../../../download/presentation/widgets/download_status_card.dart';
import '../widgets/home_section_header.dart';
import '../widgets/manual_url_save_card.dart';
import '../widgets/recent_download_tile.dart';

/// Home screen — download dashboard (SRS §6.2 / §7).
///
/// Business flow: Facebook/Instagram → Share → Social → backend processes →
/// download stored → visible here.
///
/// Placeholder catalog only until GET /api/v1/media is wired. Save does not
/// create downloads.
class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  final _urlController = TextEditingController();

  /// When true, shows the empty dashboard (no catalog items).
  /// Used to preview Share → Social empty copy without backend data.
  var _previewEmpty = false;

  @override
  void dispose() {
    _urlController.dispose();
    super.dispose();
  }

  Future<void> _pasteFromClipboard() async {
    final data = await Clipboard.getData(Clipboard.kTextPlain);
    final text = data?.text?.trim();
    if (text == null || text.isEmpty) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Clipboard is empty.')),
      );
      return;
    }
    setState(() => _urlController.text = text);
  }

  void _onSave() {
    final url = _urlController.text.trim();
    if (url.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Paste a supported Instagram or Facebook URL.'),
        ),
      );
      return;
    }
    // Placeholder only — does not create a download or call the API.
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Save will call POST /api/v1/media in a later sprint.'),
      ),
    );
  }

  void _onRetryPlaceholder() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Retry will call POST /media/{id}/retry later.'),
      ),
    );
  }

  void _onDeletePlaceholder() {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Delete will call DELETE /media/{id} later.'),
      ),
    );
  }

  void _openMediaPlaceholder(String id) {
    context.push(RoutePaths.mediaDetailPath(id));
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final width = MediaQuery.sizeOf(context).width;
    final horizontal = width >= 900 ? 32.0 : 16.0;

    // UI placeholders only — replace with API media list later.
    final List<MediaItemPreview> statusItems = _previewEmpty
        ? const <MediaItemPreview>[]
        : <MediaItemPreview>[
            ...UiPlaceholderCatalog.pending,
            ...UiPlaceholderCatalog.failed,
          ];
    final List<MediaItemPreview> recentItems = _previewEmpty
        ? const <MediaItemPreview>[]
        : UiPlaceholderCatalog.recentCompleted;
    final isEmpty = statusItems.isEmpty && recentItems.isEmpty;

    return Scaffold(
      appBar: AppBar(
        title: const Text(AppConstants.appName),
        actions: [
          IconButton(
            tooltip: _previewEmpty
                ? 'Show placeholder dashboard'
                : 'Show empty dashboard',
            onPressed: () => setState(() => _previewEmpty = !_previewEmpty),
            icon: Icon(
              _previewEmpty
                  ? Icons.dashboard_outlined
                  : Icons.inbox_outlined,
            ),
          ),
        ],
      ),
      body: SafeArea(
        child: Align(
          alignment: Alignment.topCenter,
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 720),
            child: ListView(
              padding: EdgeInsets.fromLTRB(horizontal, 8, horizontal, 28),
              children: [
                Text(
                  'Your downloads',
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.w700,
                        letterSpacing: -0.2,
                      ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Reels you share to ${AppConstants.appName} appear here after the backend downloads them.',
                  style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: scheme.onSurface.withValues(alpha: 0.68),
                        height: 1.4,
                      ),
                ),
                const SizedBox(height: 20),
                ManualUrlSaveCard(
                  controller: _urlController,
                  onPaste: _pasteFromClipboard,
                  onSave: _onSave,
                ),
                const SizedBox(height: 28),
                if (isEmpty)
                  const AppEmptyState(
                    icon: Icons.ios_share_rounded,
                    title: 'No downloads yet',
                    message:
                        'Watch a reel on Facebook or Instagram, tap Share, '
                        'and choose Social. When the download finishes, it '
                        'shows up on this dashboard. You can also paste a URL above.',
                  )
                else ...[
                  const HomeSectionHeader(
                    title: 'Download status',
                    subtitle:
                        'Preparing, queued, downloading, processing, and failed',
                  ),
                  const SizedBox(height: 10),
                  ...statusItems.map(
                    (item) => Padding(
                      padding: const EdgeInsets.only(bottom: 10),
                      child: DownloadStatusCard(
                        item: item,
                        onTap: () => _openMediaPlaceholder(item.id),
                        onRetry: item.status == MediaStatus.failed
                            ? _onRetryPlaceholder
                            : null,
                        onDelete: item.status == MediaStatus.failed
                            ? _onDeletePlaceholder
                            : null,
                      ),
                    ),
                  ),
                  const SizedBox(height: 18),
                  const HomeSectionHeader(
                    title: 'Recent downloads',
                    subtitle: 'Completed reels ready to open',
                  ),
                  const SizedBox(height: 10),
                  ...recentItems.map(
                    (item) => Padding(
                      padding: const EdgeInsets.only(bottom: 10),
                      child: RecentDownloadTile(
                        item: item,
                        onTap: () => _openMediaPlaceholder(item.id),
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }
}
