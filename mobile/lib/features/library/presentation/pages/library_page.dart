import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/router/route_paths.dart';
import '../../../../shared/data/ui_placeholder_catalog.dart';
import '../../../../shared/models/media_item_preview.dart';
import '../../../../shared/models/media_platform.dart';
import '../../../../shared/models/media_status.dart';
import '../../../../shared/widgets/app_empty_state.dart';
import '../widgets/library_media_card.dart';
import '../widgets/library_toolbar.dart';

enum LibraryLayoutMode { grid, list }

enum LibrarySortMode { newest, oldest, status }

/// Library / Downloads screen (SRS §7 / FR-013 UI shell).
class LibraryPage extends StatefulWidget {
  const LibraryPage({super.key});

  @override
  State<LibraryPage> createState() => _LibraryPageState();
}

class _LibraryPageState extends State<LibraryPage> {
  final _searchController = TextEditingController();
  LibraryLayoutMode _layout = LibraryLayoutMode.grid;
  LibrarySortMode _sort = LibrarySortMode.newest;
  MediaPlatform? _platformFilter;
  MediaStatus? _statusFilter;

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  List<MediaItemPreview> get _filtered {
    var items = List<MediaItemPreview>.from(UiPlaceholderCatalog.all);
    final query = _searchController.text.trim().toLowerCase();

    if (query.isNotEmpty) {
      items = items
          .where(
            (m) =>
                m.displayTitle.toLowerCase().contains(query) ||
                m.originalUrl.toLowerCase().contains(query) ||
                m.platform.label.toLowerCase().contains(query),
          )
          .toList();
    }
    if (_platformFilter != null) {
      items = items.where((m) => m.platform == _platformFilter).toList();
    }
    if (_statusFilter != null) {
      items = items.where((m) => m.status == _statusFilter).toList();
    }

    switch (_sort) {
      case LibrarySortMode.newest:
        items.sort((a, b) => b.savedAt.compareTo(a.savedAt));
      case LibrarySortMode.oldest:
        items.sort((a, b) => a.savedAt.compareTo(b.savedAt));
      case LibrarySortMode.status:
        items.sort((a, b) => a.status.index.compareTo(b.status.index));
    }
    return items;
  }

  Future<void> _openFilterSheet() async {
    MediaPlatform? platform = _platformFilter;
    MediaStatus? status = _statusFilter;

    await showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (context) {
        return StatefulBuilder(
          builder: (context, setModalState) {
            return Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 28),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('Filter', style: Theme.of(context).textTheme.titleLarge),
                  const SizedBox(height: 16),
                  Text('Platform', style: Theme.of(context).textTheme.titleSmall),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    children: [
                      FilterChip(
                        label: const Text('All'),
                        selected: platform == null,
                        onSelected: (_) => setModalState(() => platform = null),
                      ),
                      ...MediaPlatform.values.map(
                        (p) => FilterChip(
                          label: Text(p.label),
                          selected: platform == p,
                          onSelected: (_) => setModalState(() => platform = p),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  Text('Status', style: Theme.of(context).textTheme.titleSmall),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      FilterChip(
                        label: const Text('All'),
                        selected: status == null,
                        onSelected: (_) => setModalState(() => status = null),
                      ),
                      ...MediaStatus.values.map(
                        (s) => FilterChip(
                          label: Text(_statusLabel(s)),
                          selected: status == s,
                          onSelected: (_) => setModalState(() => status = s),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 20),
                  FilledButton(
                    onPressed: () {
                      setState(() {
                        _platformFilter = platform;
                        _statusFilter = status;
                      });
                      Navigator.of(context).pop();
                    },
                    child: const Text('Apply filters'),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }

  Future<void> _openSortMenu() async {
    final selected = await showModalBottomSheet<LibrarySortMode>(
      context: context,
      showDragHandle: true,
      builder: (context) {
        return SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              ListTile(
                title: Text('Sort', style: Theme.of(context).textTheme.titleLarge),
              ),
              ListTile(
                leading: Icon(
                  _sort == LibrarySortMode.newest
                      ? Icons.radio_button_checked
                      : Icons.radio_button_off,
                ),
                title: const Text('Newest first'),
                onTap: () => Navigator.pop(context, LibrarySortMode.newest),
              ),
              ListTile(
                leading: Icon(
                  _sort == LibrarySortMode.oldest
                      ? Icons.radio_button_checked
                      : Icons.radio_button_off,
                ),
                title: const Text('Oldest first'),
                onTap: () => Navigator.pop(context, LibrarySortMode.oldest),
              ),
              ListTile(
                leading: Icon(
                  _sort == LibrarySortMode.status
                      ? Icons.radio_button_checked
                      : Icons.radio_button_off,
                ),
                title: const Text('By status'),
                onTap: () => Navigator.pop(context, LibrarySortMode.status),
              ),
              const SizedBox(height: 8),
            ],
          ),
        );
      },
    );
    if (selected != null) {
      setState(() => _sort = selected);
    }
  }

  String _statusLabel(MediaStatus status) => switch (status) {
        MediaStatus.preparing => 'Preparing',
        MediaStatus.queued => 'Queued',
        MediaStatus.downloading => 'Downloading',
        MediaStatus.processing => 'Processing',
        MediaStatus.completed => 'Completed',
        MediaStatus.failed => 'Failed',
      };

  @override
  Widget build(BuildContext context) {
    final items = _filtered;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Library'),
        actions: [
          IconButton(
            tooltip: _layout == LibraryLayoutMode.grid ? 'List layout' : 'Grid layout',
            onPressed: () {
              setState(() {
                _layout = _layout == LibraryLayoutMode.grid
                    ? LibraryLayoutMode.list
                    : LibraryLayoutMode.grid;
              });
            },
            icon: Icon(
              _layout == LibraryLayoutMode.grid
                  ? Icons.view_list_rounded
                  : Icons.grid_view_rounded,
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: LibraryToolbar(
              searchController: _searchController,
              onSearchChanged: (_) => setState(() {}),
              onFilter: _openFilterSheet,
              onSort: _openSortMenu,
            ),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: items.isEmpty
                ? const AppEmptyState(
                    icon: Icons.video_library_outlined,
                    title: 'No media found',
                    message:
                        'Saved reels appear here. Adjust search or filters, or save a new URL from Home.',
                  )
                : _layout == LibraryLayoutMode.grid
                    ? GridView.builder(
                        padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
                        gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
                          maxCrossAxisExtent: 220,
                          mainAxisSpacing: 12,
                          crossAxisSpacing: 12,
                          childAspectRatio: 0.68,
                        ),
                        itemCount: items.length,
                        itemBuilder: (context, index) {
                          final item = items[index];
                          return LibraryMediaCard(
                            item: item,
                            dense: false,
                            onTap: () =>
                                context.push(RoutePaths.mediaDetailPath(item.id)),
                          );
                        },
                      )
                    : ListView.separated(
                        padding: const EdgeInsets.fromLTRB(16, 8, 16, 28),
                        itemCount: items.length,
                        separatorBuilder: (_, _) => const SizedBox(height: 10),
                        itemBuilder: (context, index) {
                          final item = items[index];
                          return LibraryMediaCard(
                            item: item,
                            dense: true,
                            onTap: () =>
                                context.push(RoutePaths.mediaDetailPath(item.id)),
                          );
                        },
                      ),
          ),
        ],
      ),
    );
  }
}
