import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/constants/app_constants.dart';
import '../../../../core/router/route_paths.dart';
import '../widgets/settings_section.dart';

/// Settings screen (SRS §7) — UI placeholders only.
class SettingsPage extends StatelessWidget {
  const SettingsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(title: const Text('Settings')),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 28,
                    backgroundColor: scheme.primaryContainer,
                    child: Icon(Icons.person_rounded, color: scheme.onPrimaryContainer),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'Signed-in user',
                          style: Theme.of(context).textTheme.titleMedium,
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'user@example.com',
                          style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                                color: scheme.onSurface.withValues(alpha: 0.62),
                              ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'Profile placeholder — auth session later',
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                                color: scheme.onSurface.withValues(alpha: 0.5),
                              ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 20),
          SettingsSection(
            title: 'Storage',
            children: [
              ListTile(
                leading: const Icon(Icons.cloud_outlined),
                title: const Text('Storage usage'),
                subtitle: const Text('Library media stored on the server'),
                trailing: Text(
                  '— MB',
                  style: Theme.of(context).textTheme.titleSmall,
                ),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Storage usage will load from the API later.'),
                    ),
                  );
                },
              ),
              ListTile(
                leading: const Icon(Icons.cached_rounded),
                title: const Text('Cache'),
                subtitle: const Text('Clear local playback cache'),
                trailing: const Icon(Icons.chevron_right_rounded),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Cache clearing will be available later.'),
                    ),
                  );
                },
              ),
            ],
          ),
          const SizedBox(height: 16),
          SettingsSection(
            title: 'Legal & privacy',
            children: [
              ListTile(
                leading: const Icon(Icons.privacy_tip_outlined),
                title: const Text('Privacy'),
                subtitle: Text('How ${AppConstants.appName} handles your data'),
                trailing: const Icon(Icons.open_in_new_rounded, size: 18),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Privacy policy link placeholder.'),
                    ),
                  );
                },
              ),
              ListTile(
                leading: const Icon(Icons.description_outlined),
                title: const Text('Terms'),
                subtitle: const Text('Terms of use for saving and storing media'),
                trailing: const Icon(Icons.open_in_new_rounded, size: 18),
                onTap: () {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Terms of use link placeholder.'),
                    ),
                  );
                },
              ),
            ],
          ),
          const SizedBox(height: 16),
          SettingsSection(
            title: 'Preferences',
            children: [
              SwitchListTile(
                secondary: const Icon(Icons.dark_mode_outlined),
                title: const Text('Follow system theme'),
                subtitle: const Text('Light and dark themes are both supported'),
                value: true,
                onChanged: (_) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text('Theme preference will be persisted later.'),
                    ),
                  );
                },
              ),
            ],
          ),
          const SizedBox(height: 24),
          FilledButton.tonalIcon(
            onPressed: () {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Logout will clear the session in a later sprint.'),
                ),
              );
              context.go(RoutePaths.login);
            },
            icon: const Icon(Icons.logout_rounded),
            label: const Text('Log out'),
          ),
          const SizedBox(height: 12),
          Text(
            '${AppConstants.appName} · UI foundation',
            textAlign: TextAlign.center,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: scheme.onSurface.withValues(alpha: 0.45),
                ),
          ),
        ],
      ),
    );
  }
}
