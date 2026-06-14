import 'package:flutter/material.dart';
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
    this.title = 'Something went wrong',
    this.subtitle,
    this.buttonLabel = 'Try Again',
    this.onRetry,
    this.onDismiss,
    this.icon = Icons.error_outline_rounded,
    this.iconColor,
  });

  const ErrorWidgetView.network({
    super.key,
    this.buttonLabel = 'Retry',
    this.onRetry,
    this.onDismiss,
  })  : title = 'No internet connection',
        subtitle = 'Check your connection and try again.',
        icon = Icons.wifi_off_rounded,
        iconColor = AppColors.warning;

  const ErrorWidgetView.server({
    super.key,
    this.buttonLabel = 'Try Again',
    this.onRetry,
    this.onDismiss,
  })  : title = 'Server error',
        subtitle = 'Our servers are having trouble. Please try again later.',
        icon = Icons.cloud_off_rounded,
        iconColor = AppColors.error;

  const ErrorWidgetView.notFound({
    super.key,
    this.buttonLabel = 'Go Back',
    this.onRetry,
    this.onDismiss,
  })  : title = 'Not found',
        subtitle = 'The page you\'re looking for doesn\'t exist or has been removed.',
        icon = Icons.search_off_rounded,
        iconColor = AppColors.grey500;

  const ErrorWidgetView.location({
    super.key,
    this.buttonLabel = 'Enable Location',
    this.onRetry,
    this.onDismiss,
  })  : title = 'Location required',
        subtitle = 'Enable location services to find nearby offers.',
        icon = Icons.location_off_rounded,
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
                      label: 'Dismiss',
                      onPressed: onDismiss,
                      type: CustomButtonType.text,
                      isFullWidth: false,
                    ),
                  ),
                if (onRetry != null)
                  CustomButton(
                    label: buttonLabel ?? 'Try Again',
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
