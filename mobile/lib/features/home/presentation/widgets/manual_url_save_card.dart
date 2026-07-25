import 'package:flutter/material.dart';

/// Manual URL paste fallback (SRS §6.2 / FR-002).
class ManualUrlSaveCard extends StatelessWidget {
  const ManualUrlSaveCard({
    super.key,
    required this.controller,
    required this.onPaste,
    required this.onSave,
  });

  final TextEditingController controller;
  final VoidCallback onPaste;
  final VoidCallback onSave;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Save a reel',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: 4),
            Text(
              'Paste an Instagram or Facebook URL when share is unavailable.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: scheme.onSurface.withValues(alpha: 0.62),
                  ),
            ),
            const SizedBox(height: 14),
            TextField(
              controller: controller,
              keyboardType: TextInputType.url,
              textInputAction: TextInputAction.done,
              decoration: const InputDecoration(
                labelText: 'Media URL',
                hintText: 'https://www.instagram.com/reel/...',
                prefixIcon: Icon(Icons.link_rounded),
              ),
              onSubmitted: (_) => onSave(),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: onPaste,
                    icon: const Icon(Icons.content_paste_rounded),
                    label: const Text('Paste'),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: FilledButton.icon(
                    onPressed: onSave,
                    icon: const Icon(Icons.download_rounded),
                    label: const Text('Save'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
