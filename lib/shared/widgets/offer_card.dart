import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/helpers.dart';

class OfferCard extends StatelessWidget {
  final String id;
  final String imageUrl;
  final String title;
  final String storeName;
  final double originalPrice;
  final double discountPrice;
  final int remainingQuantity;
  final DateTime expiryTime;
  final double distance;
  final double rating;
  final VoidCallback? onTap;
  final VoidCallback? onFavorite;
  final bool isFavorite;

  const OfferCard({
    super.key,
    required this.id,
    required this.imageUrl,
    required this.title,
    required this.storeName,
    required this.originalPrice,
    required this.discountPrice,
    required this.remainingQuantity,
    required this.expiryTime,
    required this.distance,
    required this.rating,
    this.onTap,
    this.onFavorite,
    this.isFavorite = false,
  });

  @override
  Widget build(BuildContext context) {
    final discountPercent = Helpers.calculateDiscountPercentage(
      originalPrice,
      discountPrice,
    );
    final isExpired = Helpers.isExpired(expiryTime);

    return GestureDetector(
      onTap: onTap,
      child: Container(
        decoration: BoxDecoration(
          color: Theme.of(context).cardColor,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withAlpha(13),
              blurRadius: 10,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            _buildImage(context, discountPercent, isExpired),
            _buildDetails(context),
          ],
        ),
      ),
    );
  }

  Widget _buildImage(BuildContext context, String discountPercent, bool isExpired) {
    return ClipRRect(
      borderRadius: const BorderRadius.vertical(top: Radius.circular(16)),
      child: SizedBox(
        height: 140,
        width: double.infinity,
        child: Stack(
          children: [
            CachedNetworkImage(
              imageUrl: imageUrl,
              width: double.infinity,
              height: 140,
              fit: BoxFit.cover,
              placeholder: (_, _) => Container(
                color: Theme.of(context).colorScheme.surfaceContainerHighest,
              ),
              errorWidget: (_, _, _) => Container(
                color: Theme.of(context).colorScheme.surfaceContainerHighest,
                child: const Icon(Icons.image_outlined, size: 40),
              ),
            ),
            Positioned(
              top: 8,
              left: 8,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.discountRed,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  '-$discountPercent',
                  style: AppTextStyles.discount.copyWith(color: Colors.white),
                ),
              ),
            ),
            if (onFavorite != null)
              Positioned(
                top: 8,
                right: 8,
                child: GestureDetector(
                  onTap: onFavorite,
                  child: Container(
                    padding: const EdgeInsets.all(6),
                    decoration: BoxDecoration(
                      color: Colors.black.withAlpha(51),
                      shape: BoxShape.circle,
                    ),
                    child: Icon(
                      isFavorite ? Icons.favorite : Icons.favorite_border,
                      color: isFavorite ? AppColors.discountRed : Colors.white,
                      size: 18,
                    ),
                  ),
                ),
              ),
            if (isExpired)
              Positioned.fill(
                child: Container(
                  color: Colors.black.withAlpha(128),
                  child: Center(
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                      decoration: BoxDecoration(
                        color: Colors.black.withAlpha(179),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(
                        AppLocalizations.of(context)!.expired,
                        style: AppTextStyles.labelLarge.copyWith(color: Colors.white),
                      ),
                    ),
                  ),
                ),
              ),
            Positioned(
              bottom: 8,
              right: 8,
              child: _buildCountdown(context, isExpired),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildCountdown(BuildContext context, bool isExpired) {
    final timeText = isExpired ? AppLocalizations.of(context)!.expired : Helpers.formatRemainingTime(expiryTime, context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: Colors.black.withAlpha(153),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.access_time, size: 12, color: isExpired ? Colors.grey : Colors.white),
          const SizedBox(width: 4),
          Text(
            timeText,
            style: AppTextStyles.caption.copyWith(
              color: isExpired ? Colors.grey : Colors.white,
              fontWeight: FontWeight.w600,
              fontSize: 11,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDetails(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            title,
            style: AppTextStyles.productName,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
          const SizedBox(height: 4),
          Row(
            children: [
              const Icon(Icons.store_outlined, size: 14, color: AppColors.grey500),
              const SizedBox(width: 4),
              Expanded(
                child: Text(
                  storeName,
                  style: AppTextStyles.storeName,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Row(
                children: [
                  const Icon(Icons.star_rounded, size: 16, color: AppColors.secondary),
                  const SizedBox(width: 2),
                  Text(
                    rating.toStringAsFixed(1),
                    style: AppTextStyles.labelMedium.copyWith(
                      color: AppColors.grey700,
                    ),
                  ),
                ],
              ),
              const Spacer(),
              Row(
                children: [
                  const Icon(Icons.location_on_outlined, size: 14, color: AppColors.grey500),
                  const SizedBox(width: 2),
                  Text(
                    Helpers.formatDistance(distance),
                    style: AppTextStyles.caption,
                  ),
                ],
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Text(
                Helpers.formatOriginalPrice(originalPrice),
                style: AppTextStyles.caption.copyWith(
                  decoration: TextDecoration.lineThrough,
                  color: AppColors.grey400,
                ),
              ),
              const SizedBox(width: 6),
              Text(
                Helpers.formatPrice(discountPrice),
                style: AppTextStyles.price.copyWith(color: AppColors.primary),
              ),
              const Spacer(),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: remainingQuantity <= 5
                      ? AppColors.discountRed.withAlpha(26)
                      : AppColors.primary.withAlpha(26),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(
                  AppLocalizations.of(context)!.remainingLeft(remainingQuantity),
                  style: AppTextStyles.caption.copyWith(
                    color: remainingQuantity <= 5
                        ? AppColors.discountRed
                        : AppColors.primary,
                    fontWeight: FontWeight.w600,
                    fontSize: 11,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
