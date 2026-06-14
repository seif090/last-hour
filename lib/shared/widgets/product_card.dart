import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import '../../core/theme/app_text_styles.dart';

class ProductCard extends StatelessWidget {
  final String id;
  final String imageUrl;
  final String name;
  final String description;
  final double price;
  final double? originalPrice;
  final int quantity;
  final VoidCallback? onTap;
  final VoidCallback? onAddToCart;

  const ProductCard({
    super.key,
    required this.id,
    required this.imageUrl,
    required this.name,
    required this.description,
    required this.price,
    this.originalPrice,
    required this.quantity,
    this.onTap,
    this.onAddToCart,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return GestureDetector(
      onTap: onTap,
      child: Container(
        decoration: BoxDecoration(
          color: theme.cardColor,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withAlpha(13),
              blurRadius: 10,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Row(
          children: [
            _buildImage(theme),
            _buildDetails(theme),
          ],
        ),
      ),
    );
  }

  Widget _buildImage(ThemeData theme) {
    return ClipRRect(
      borderRadius: const BorderRadius.horizontal(left: Radius.circular(16)),
      child: CachedNetworkImage(
        imageUrl: imageUrl,
        width: 100,
        height: 100,
        fit: BoxFit.cover,
        placeholder: (_, _) => Container(
          width: 100,
          height: 100,
          color: theme.colorScheme.surfaceContainerHighest,
        ),
        errorWidget: (_, _, _) => Container(
          width: 100,
          height: 100,
          color: theme.colorScheme.surfaceContainerHighest,
          child: const Icon(Icons.image_outlined, size: 32),
        ),
      ),
    );
  }

  Widget _buildDetails(ThemeData theme) {
    return Expanded(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              name,
              style: AppTextStyles.productName,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: 4),
            Text(
              description,
              style: AppTextStyles.caption,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                if (originalPrice != null)
                  Padding(
                    padding: const EdgeInsets.only(right: 6),
                    child: Text(
                      '\$${originalPrice!.toStringAsFixed(2)}',
                      style: AppTextStyles.caption.copyWith(
                        decoration: TextDecoration.lineThrough,
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ),
                Text(
                  '\$${price.toStringAsFixed(2)}',
                  style: AppTextStyles.price.copyWith(
                    color: theme.colorScheme.primary,
                  ),
                ),
                const Spacer(),
                if (quantity > 0 && onAddToCart != null)
                  Material(
                    color: theme.colorScheme.primary,
                    borderRadius: BorderRadius.circular(8),
                    child: InkWell(
                      onTap: onAddToCart,
                      borderRadius: BorderRadius.circular(8),
                      child: const Padding(
                        padding: EdgeInsets.all(6),
                        child: Icon(
                          Icons.add_shopping_cart_outlined,
                          size: 18,
                          color: Colors.white,
                        ),
                      ),
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
