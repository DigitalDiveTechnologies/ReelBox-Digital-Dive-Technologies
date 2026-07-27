import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_gradients.dart';
import '../../../../core/theme/app_radius.dart';
import '../../../../core/theme/app_spacing.dart';
import '../../data/supported_reel_url_validator.dart';

/// Bottom sheet for pasting / typing an Instagram or Facebook reel URL.
Future<String?> showPasteLinkSheet(BuildContext context) {
  return showModalBottomSheet<String>(
    context: context,
    isScrollControlled: true,
    backgroundColor: Colors.transparent,
    builder: (context) => const _PasteLinkSheet(),
  );
}

class _PasteLinkSheet extends StatefulWidget {
  const _PasteLinkSheet();

  @override
  State<_PasteLinkSheet> createState() => _PasteLinkSheetState();
}

class _PasteLinkSheetState extends State<_PasteLinkSheet> {
  final _controller = TextEditingController();
  String? _error;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _pasteFromClipboard() async {
    final data = await Clipboard.getData(Clipboard.kTextPlain);
    final text = data?.text?.trim();
    if (!mounted) return;
    if (text == null || text.isEmpty) {
      setState(() => _error = 'Clipboard is empty.');
      return;
    }
    setState(() {
      _controller.text = text;
      _controller.selection = TextSelection.collapsed(offset: text.length);
      _error = null;
    });
  }

  void _submit() {
    final result = SupportedReelUrlValidator.validate(_controller.text);
    if (!result.isValid) {
      setState(() => _error = result.errorMessage);
      return;
    }
    Navigator.of(context).pop(result.url);
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.viewInsetsOf(context).bottom;
    final safeBottom = MediaQuery.paddingOf(context).bottom;

    return Padding(
      padding: EdgeInsets.only(bottom: bottomInset),
      child: Container(
        width: double.infinity,
        decoration: BoxDecoration(
          color: AppColors.splashSheet,
          borderRadius: AppRadius.sheetTop,
        ),
        padding: EdgeInsets.fromLTRB(
          AppSpacing.xl,
          AppSpacing.md,
          AppSpacing.xl,
          AppSpacing.xl + safeBottom,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Center(
              child: Container(
                width: AppSpacing.splashHandleWidth,
                height: AppSpacing.splashHandleHeight,
                decoration: BoxDecoration(
                  color: AppColors.splashHandle.withValues(alpha: 0.85),
                  borderRadius: AppRadius.circularPill,
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            const Text(
              'Paste a link',
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w700,
                color: AppColors.splashTextPrimary,
              ),
            ),
            const SizedBox(height: AppSpacing.xs),
            Text(
              'Instagram or Facebook reel URL',
              style: TextStyle(
                fontSize: 13,
                color: AppColors.splashTextMuted.withValues(alpha: 0.95),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            TextField(
              controller: _controller,
              autofocus: true,
              keyboardType: TextInputType.url,
              textInputAction: TextInputAction.done,
              style: const TextStyle(
                color: AppColors.splashTextPrimary,
                fontSize: 15,
              ),
              cursorColor: AppColors.splashTextPrimary,
              onChanged: (_) {
                if (_error != null) setState(() => _error = null);
              },
              onSubmitted: (_) => _submit(),
              decoration: InputDecoration(
                hintText: 'https://www.instagram.com/reel/…',
                hintStyle: TextStyle(
                  color: AppColors.splashTextMuted.withValues(alpha: 0.7),
                ),
                filled: true,
                fillColor: AppColors.splashChipFill.withValues(alpha: 0.85),
                border: OutlineInputBorder(
                  borderRadius: AppRadius.circularCard,
                  borderSide: BorderSide(
                    color: AppColors.splashChipBorder.withValues(alpha: 0.7),
                  ),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: AppRadius.circularCard,
                  borderSide: BorderSide(
                    color: AppColors.splashChipBorder.withValues(alpha: 0.7),
                  ),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: AppRadius.circularCard,
                  borderSide: BorderSide(
                    color: AppColors.brandPurple.withValues(alpha: 0.8),
                  ),
                ),
                errorText: _error,
                errorMaxLines: 3,
                errorStyle: const TextStyle(color: AppColors.statusFailed),
                suffixIcon: IconButton(
                  tooltip: 'Paste from clipboard',
                  onPressed: _pasteFromClipboard,
                  icon: const Icon(
                    Icons.content_paste_rounded,
                    color: AppColors.splashTextPrimary,
                  ),
                ),
              ),
            ),
            const SizedBox(height: AppSpacing.md),
            SizedBox(
              height: AppSpacing.buttonHeight,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  gradient: AppGradients.brandCta,
                  borderRadius: AppRadius.circularButton,
                ),
                child: Material(
                  color: Colors.transparent,
                  child: InkWell(
                    onTap: _submit,
                    borderRadius: AppRadius.circularButton,
                    child: const Center(
                      child: Text(
                        'Save reel',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.w700,
                          color: AppColors.splashTextPrimary,
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
