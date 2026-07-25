import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../providers/share_providers.dart';

/// Displays an inbound shared URL from `/share?url=`.
///
/// TODO: Also open this page from Android Share Intent / iOS Share Extension.
class SharePage extends ConsumerWidget {
  const SharePage({super.key, this.sharedUrl});

  /// Raw `url` query parameter from GoRouter.
  final String? sharedUrl;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final request = ref.watch(shareControllerProvider).receiveSharedUrl(sharedUrl);

    return Scaffold(
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: request == null
              ? const Text('No shared URL received.')
              : Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Text('Received URL'),
                    const SizedBox(height: 8),
                    Text(
                      request.url,
                      textAlign: TextAlign.center,
                    ),
                  ],
                ),
        ),
      ),
    );
  }
}
