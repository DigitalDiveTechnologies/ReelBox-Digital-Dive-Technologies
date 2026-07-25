import 'package:flutter/material.dart';

import '../../core/theme/app_colors.dart';
import 'media_status.dart';

extension MediaStatusUi on MediaStatus {
  String get label => switch (this) {
        MediaStatus.preparing => 'Preparing',
        MediaStatus.queued => 'Queued',
        MediaStatus.downloading => 'Downloading',
        MediaStatus.processing => 'Processing',
        MediaStatus.completed => 'Completed',
        MediaStatus.failed => 'Failed',
      };

  String get description => switch (this) {
        MediaStatus.preparing => 'Validating the URL and creating a media item.',
        MediaStatus.queued => 'Accepted and waiting for a download worker.',
        MediaStatus.downloading => 'Download is active.',
        MediaStatus.processing => 'Finalizing validation, thumbnail, and upload.',
        MediaStatus.completed => 'Ready to play from your library.',
        MediaStatus.failed => 'Something went wrong. Retry when available.',
      };

  IconData get icon => switch (this) {
        MediaStatus.preparing => Icons.hourglass_top_rounded,
        MediaStatus.queued => Icons.schedule_rounded,
        MediaStatus.downloading => Icons.downloading_rounded,
        MediaStatus.processing => Icons.auto_awesome_rounded,
        MediaStatus.completed => Icons.check_circle_rounded,
        MediaStatus.failed => Icons.error_outline_rounded,
      };

  Color color(BuildContext context) => AppColors.statusColor(this);

  Color containerColor(BuildContext context) =>
      AppColors.statusContainer(this, Theme.of(context).brightness);
}
