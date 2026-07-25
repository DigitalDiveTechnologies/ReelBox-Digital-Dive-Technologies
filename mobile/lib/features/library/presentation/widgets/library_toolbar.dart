import 'package:flutter/material.dart';

class LibraryToolbar extends StatelessWidget {
  const LibraryToolbar({
    super.key,
    required this.searchController,
    required this.onSearchChanged,
    required this.onFilter,
    required this.onSort,
  });

  final TextEditingController searchController;
  final ValueChanged<String> onSearchChanged;
  final VoidCallback onFilter;
  final VoidCallback onSort;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: TextField(
            controller: searchController,
            onChanged: onSearchChanged,
            decoration: const InputDecoration(
              hintText: 'Search library',
              prefixIcon: Icon(Icons.search_rounded),
              isDense: true,
            ),
          ),
        ),
        const SizedBox(width: 8),
        IconButton.filledTonal(
          tooltip: 'Filter',
          onPressed: onFilter,
          icon: const Icon(Icons.filter_list_rounded),
        ),
        const SizedBox(width: 4),
        IconButton.filledTonal(
          tooltip: 'Sort',
          onPressed: onSort,
          icon: const Icon(Icons.sort_rounded),
        ),
      ],
    );
  }
}
