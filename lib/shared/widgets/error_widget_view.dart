import 'package:flutter/material.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import '../../core/theme/app_colors.dart';
import 'custom_button.dart';

class ErrorWidgetView extends StatelessWidget {
  final String title;
  final String? subtitle;
  final String? buttonLabel;
  final VoidCallback? onRetry;
  final VoidCallback? onDismiss;
  final IconData icon;
  final Color? iconColor;

  const ErrorWidgetView({
    super.key,
    required this.title,
    this.subtitle,
    this.buttonLabel,
    this.onRetry,
    this.onDismiss,
    this.icon = Icons.error_outline_rounded,
    this.iconColor,
  });

  ErrorWidgetView.network({
    super.key,
    required this.title,
    required this.subtitle,
    this.buttonLabel,
    this.onRetry,
    this.onDismiss,
  })  : icon = Icons.wifi_off_rounded,
        iconColor = AppColors.warning;

  ErrorWidgetView.server({
    super.key,
    required this.title,
    required this.subtitle,
    this.buttonLabel,
    this.onRetry,
    this.onDismiss,
  })  : icon = Icons.cloud_off_rounded,
        iconColor = AppColors.error;

  ErrorWidgetView.notFound({
    super.key,
    required this.title,
    required this.subtitle,
    this.buttonLabel,
    this.onRetry,
    this.onDismiss,
  })  : icon = Icons.search_off_rounded,
        iconColor = AppColors.grey500;

  ErrorWidgetView.location({
    super.key,
    required this.title,
    required this.subtitle,
    this.buttonLabel,
    this.onRetry,
    this.onDismiss,
  })  : icon = Icons.location_off_rounded,
        iconColor = AppColors.warning;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 100,
              height: 100,
              decoration: BoxDecoration(
                color: (iconColor ?? AppColors.error).withAlpha(26),
                shape: BoxShape.circle,
              ),
              child: Icon(
                icon,
                size: 48,
                color: iconColor ?? AppColors.error,
              ),
            ),
            const SizedBox(height: 24),
            Text(
              title,
              style: theme.textTheme.titleLarge?.copyWith(
                fontWeight: FontWeight.w600,
              ),
              textAlign: TextAlign.center,
            ),
            if (subtitle != null) ...[
              const SizedBox(height: 8),
              Text(
                subtitle!,
                style: theme.textTheme.bodyMedium?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
                textAlign: TextAlign.center,
              ),
            ],
            const SizedBox(height: 24),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                if (onDismiss != null)
                  Padding(
                    padding: const EdgeInsets.only(right: 12),
                    child: CustomButton(
                      label: AppLocalizations.of(context)!.dismiss,
                      onPressed: onDismiss,
                      type: CustomButtonType.text,
                      isFullWidth: false,
                    ),
                  ),
                if (onRetry != null)
                  CustomButton(
                    label: buttonLabel ?? AppLocalizations.of(context)!.tryAgain,
                    onPressed: onRetry,
                    isFullWidth: false,
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
