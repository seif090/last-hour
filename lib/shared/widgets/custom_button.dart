import 'package:flutter/material.dart';

enum CustomButtonType { primary, secondary, outline, text }

class CustomButton extends StatelessWidget {
  final String label;
  final VoidCallback? onPressed;
  final CustomButtonType type;
  final IconData? icon;
  final Widget? iconWidget;
  final bool isLoading;
  final bool isFullWidth;
  final double? height;
  final double? width;
  final double? borderRadius;
  final Color? backgroundColor;
  final Color? foregroundColor;
  final Color? borderColor;
  final EdgeInsetsGeometry? padding;
  final TextStyle? textStyle;

  const CustomButton({
    super.key,
    required this.label,
    this.onPressed,
    this.type = CustomButtonType.primary,
    this.icon,
    this.iconWidget,
    this.isLoading = false,
    this.isFullWidth = true,
    this.height,
    this.width,
    this.borderRadius,
    this.backgroundColor,
    this.foregroundColor,
    this.borderColor,
    this.padding,
    this.textStyle,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final effectiveHeight = height ?? 52.0;
    final effectiveRadius = borderRadius ?? 12.0;

    final buttonChild = _buildChild(theme);

    switch (type) {
      case CustomButtonType.primary:
        return _buildElevatedButton(theme, effectiveHeight, effectiveRadius, buttonChild);
      case CustomButtonType.secondary:
        return _buildSecondaryButton(theme, effectiveHeight, effectiveRadius, buttonChild);
      case CustomButtonType.outline:
        return _buildOutlineButton(theme, effectiveHeight, effectiveRadius, buttonChild);
      case CustomButtonType.text:
        return _buildTextButton(theme, effectiveHeight, effectiveRadius, buttonChild);
    }
  }

  Widget _buildChild(ThemeData theme) {
    if (isLoading) {
      return SizedBox(
        height: 22,
        width: 22,
        child: CircularProgressIndicator(
          strokeWidth: 2.5,
          color: foregroundColor ?? _getDefaultForeground(theme),
        ),
      );
    }

    final labelWidget = Text(
      label,
      style: textStyle ?? theme.textTheme.labelLarge?.copyWith(
        color: foregroundColor ?? _getDefaultForeground(theme),
        fontWeight: FontWeight.w600,
      ),
    );

    if (icon != null) {
      return Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 20, color: foregroundColor ?? _getDefaultForeground(theme)),
          const SizedBox(width: 8),
          labelWidget,
        ],
      );
    }

    if (iconWidget != null) {
      return Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          iconWidget!,
          const SizedBox(width: 8),
          labelWidget,
        ],
      );
    }

    return labelWidget;
  }

  Color _getDefaultForeground(ThemeData theme) {
    switch (type) {
      case CustomButtonType.primary:
        return theme.colorScheme.onPrimary;
      case CustomButtonType.secondary:
        return theme.colorScheme.onSecondary;
      case CustomButtonType.outline:
      case CustomButtonType.text:
        return theme.colorScheme.primary;
    }
  }

  Widget _buildElevatedButton(
    ThemeData theme,
    double height,
    double radius,
    Widget child,
  ) {
    return SizedBox(
      width: isFullWidth ? double.infinity : width,
      height: height,
      child: ElevatedButton(
        onPressed: isLoading ? null : onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: backgroundColor ?? theme.colorScheme.primary,
          foregroundColor: foregroundColor ?? theme.colorScheme.onPrimary,
          disabledBackgroundColor: theme.colorScheme.onSurface.withAlpha(25),
          disabledForegroundColor: theme.colorScheme.onSurface.withAlpha(77),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(radius),
          ),
          padding: padding ?? const EdgeInsets.symmetric(horizontal: 24),
          elevation: 0,
        ),
        child: child,
      ),
    );
  }

  Widget _buildSecondaryButton(
    ThemeData theme,
    double height,
    double radius,
    Widget child,
  ) {
    return SizedBox(
      width: isFullWidth ? double.infinity : width,
      height: height,
      child: ElevatedButton(
        onPressed: isLoading ? null : onPressed,
        style: ElevatedButton.styleFrom(
          backgroundColor: backgroundColor ?? theme.colorScheme.secondaryContainer,
          foregroundColor: foregroundColor ?? theme.colorScheme.onSecondaryContainer,
          disabledBackgroundColor: theme.colorScheme.onSurface.withAlpha(25),
          disabledForegroundColor: theme.colorScheme.onSurface.withAlpha(77),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(radius),
          ),
          padding: padding ?? const EdgeInsets.symmetric(horizontal: 24),
          elevation: 0,
        ),
        child: child,
      ),
    );
  }

  Widget _buildOutlineButton(
    ThemeData theme,
    double height,
    double radius,
    Widget child,
  ) {
    return SizedBox(
      width: isFullWidth ? double.infinity : width,
      height: height,
      child: OutlinedButton(
        onPressed: isLoading ? null : onPressed,
        style: OutlinedButton.styleFrom(
          foregroundColor: foregroundColor ?? theme.colorScheme.primary,
          side: BorderSide(
            color: borderColor ?? theme.colorScheme.outline,
          ),
          disabledForegroundColor: theme.colorScheme.onSurface.withAlpha(77),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(radius),
          ),
          padding: padding ?? const EdgeInsets.symmetric(horizontal: 24),
        ),
        child: child,
      ),
    );
  }

  Widget _buildTextButton(
    ThemeData theme,
    double height,
    double radius,
    Widget child,
  ) {
    return SizedBox(
      width: isFullWidth ? double.infinity : width,
      height: height,
      child: TextButton(
        onPressed: isLoading ? null : onPressed,
        style: TextButton.styleFrom(
          foregroundColor: foregroundColor ?? theme.colorScheme.primary,
          disabledForegroundColor: theme.colorScheme.onSurface.withAlpha(77),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(radius),
          ),
          padding: padding ?? const EdgeInsets.symmetric(horizontal: 24),
        ),
        child: child,
      ),
    );
  }
}
