import 'package:flutter/material.dart';
import 'package:lottie/lottie.dart';
import '../../core/constants/asset_constants.dart';
import 'custom_button.dart';

class EmptyStateWidget extends StatelessWidget {
  final String title;
  final String? subtitle;
  final String? lottieAsset;
  final String? imageAsset;
  final String? buttonLabel;
  final VoidCallback? onButtonPressed;
  final IconData? icon;

  const EmptyStateWidget({
    super.key,
    required this.title,
    this.subtitle,
    this.lottieAsset,
    this.imageAsset,
    this.buttonLabel,
    this.onButtonPressed,
    this.icon,
  });

  EmptyStateWidget.cart({
    super.key,
    required this.title,
    required this.subtitle,
    this.buttonLabel,
    this.onButtonPressed,
  })  : lottieAsset = null,
        imageAsset = AssetConstants.emptyCart,
        icon = Icons.shopping_cart_outlined;

  EmptyStateWidget.orders({
    super.key,
    required this.title,
    required this.subtitle,
    this.buttonLabel,
    this.onButtonPressed,
  })  : lottieAsset = null,
        imageAsset = AssetConstants.emptyOrders,
        icon = Icons.receipt_long_outlined;

  EmptyStateWidget.favorites({
    super.key,
    required this.title,
    required this.subtitle,
    this.buttonLabel,
    this.onButtonPressed,
  })  : lottieAsset = null,
        imageAsset = AssetConstants.emptyFavorites,
        icon = Icons.favorite_outline;

  EmptyStateWidget.search({
    super.key,
    required this.title,
    required this.subtitle,
    this.buttonLabel,
    this.onButtonPressed,
  })  : lottieAsset = null,
        imageAsset = AssetConstants.emptySearch,
        icon = Icons.search_off;

  EmptyStateWidget.offers({
    super.key,
    required this.title,
    required this.subtitle,
    this.buttonLabel,
    this.onButtonPressed,
  })  : lottieAsset = null,
        imageAsset = AssetConstants.emptyOffers,
        icon = Icons.local_offer_outlined;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            _buildIllustration(context),
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
            if (buttonLabel != null && onButtonPressed != null) ...[
              const SizedBox(height: 24),
              CustomButton(
                label: buttonLabel!,
                onPressed: onButtonPressed,
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildIllustration(BuildContext context) {
    if (lottieAsset != null) {
      return SizedBox(
        width: 200,
        height: 200,
        child: Lottie.asset(
          lottieAsset!,
          fit: BoxFit.contain,
        ),
      );
    }

    if (icon != null) {
      return Container(
        width: 120,
        height: 120,
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.primaryContainer.withAlpha(77),
          shape: BoxShape.circle,
        ),
        child: Icon(
          icon,
          size: 56,
          color: Theme.of(context).colorScheme.primary.withAlpha(153),
        ),
      );
    }

    return const SizedBox.shrink();
  }
}
