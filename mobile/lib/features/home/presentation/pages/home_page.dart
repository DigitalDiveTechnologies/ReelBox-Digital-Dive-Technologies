import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_constants.dart';
import '../../../../core/router/route_paths.dart';
import '../../../../shared/data/ui_placeholder_catalog.dart';
import '../../../../shared/widgets/app_empty_state.dart';
import '../../../download/presentation/widgets/download_status_card.dart';
import '../widgets/home_section_header.dart';
import '../widgets/manual_url_save_card.dart';
import '../widgets/recent_download_tile.dart';

/// Home screen — share entry + manual URL fallback (SRS §6.2 / §7).
///
/// TODO: Wire POST /api/v1/media and share-intent intake in later sprints.
class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  final _urlController = TextEditingController();
  bool _showEmptyDemo = false;

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
        const SnackBar(content: Text('Paste a supported Instagram or Facebook URL.')),
      );
      return;
    }
    // UI placeholder — no create-media API call.
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(
        content: Text('Save will call POST /api/v1/media in a later sprint.'),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final recent = UiPlaceholderCatalog.recentCompleted;
    final pending = UiPlaceholderCatalog.pending;
    final failed = UiPlaceholderCatalog.failed;
    final isEmpty = _showEmptyDemo;

    return Scaffold(
      appBar: AppBar(
        title: Text(AppConstants.appName),
        actions: [
          IconButton(
            tooltip: _showEmptyDemo ? 'Show sample items' : 'Show empty state',
            onPressed: () => setState(() => _showEmptyDemo = !_showEmptyDemo),
            icon: Icon(
              _showEmptyDemo ? Icons.inventory_2_outlined : Icons.inbox_outlined,
            ),
          ),
          IconButton(
            tooltip: 'Settings',
            onPressed: () => context.go(RoutePaths.settings),
            icon: const Icon(Icons.settings_outlined),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
        children: [
          ManualUrlSaveCard(
            controller: _urlController,
            onPaste: _pasteFromClipboard,
            onSave: _onSave,
          ),
          const SizedBox(height: 28),
          if (isEmpty)
            const AppEmptyState(
              icon: Icons.share_outlined,
              title: 'No downloads yet',
              message:
                  'Share a reel from Instagram or Facebook, or paste a URL above and tap Save.',
            )
          else ...[
            const HomeSectionHeader(
              title: 'Pending downloads',
              subtitle: 'Preparing, queued, downloading, and processing',
            ),
            const SizedBox(height: 10),
            ...pending.map(
              (item) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: DownloadStatusCard(
                  item: item,
                  onTap: () => context.push(RoutePaths.mediaDetailPath(item.id)),
                ),
              ),
            ),
            const SizedBox(height: 18),
            const HomeSectionHeader(
              title: 'Failed downloads',
              subtitle: 'Retry or delete when appropriate',
            ),
            const SizedBox(height: 10),
            ...failed.map(
              (item) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: DownloadStatusCard(
                  item: item,
                  onTap: () => context.push(RoutePaths.mediaDetailPath(item.id)),
                  onRetry: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Retry will call POST /media/{id}/retry later.'),
                      ),
                    );
                  },
                  onDelete: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text('Delete will call DELETE /media/{id} later.'),
                      ),
                    );
                  },
                ),
              ),
            ),
            const SizedBox(height: 18),
            const HomeSectionHeader(
              title: 'Recent downloads',
              subtitle: 'Completed items ready for playback',
            ),
            const SizedBox(height: 10),
            ...recent.map(
              (item) => Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: RecentDownloadTile(
                  item: item,
                  onTap: () => context.push(RoutePaths.mediaDetailPath(item.id)),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}
