import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import '../../core/theme/app_colors.dart';
import '../../core/theme/app_text_styles.dart';
import '../../core/utils/helpers.dart';

class StoreCard extends StatelessWidget {
  final String id;
  final String name;
  final String imageUrl;
  final String? coverImageUrl;
  final String category;
  final double rating;
  final int reviewCount;
  final double distance;
  final int activeOffersCount;
  final bool isOpen;
  final VoidCallback? onTap;
  final VoidCallback? onFavorite;
  final bool isFavorite;

  const StoreCard({
    super.key,
    required this.id,
    required this.name,
    required this.imageUrl,
    this.coverImageUrl,
    required this.category,
    required this.rating,
    required this.reviewCount,
    required this.distance,
    required this.activeOffersCount,
    required this.isOpen,
    this.onTap,
    this.onFavorite,
    this.isFavorite = false,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 160,
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
            _buildHeader(context),
            _buildInfo(context),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(BuildContext context) {
    return ClipRRect(
      borderRadius: const BorderRadius.vertical(top: Radius.circular(16)),
      child: SizedBox(
        height: 100,
        width: double.infinity,
        child: Stack(
          children: [
            CachedNetworkImage(
              imageUrl: coverImageUrl ?? imageUrl,
              width: double.infinity,
              height: 100,
              fit: BoxFit.cover,
              placeholder: (_, _) => Container(
                color: Theme.of(context).colorScheme.surfaceContainerHighest,
              ),
              errorWidget: (_, _, _) => Container(
                color: Theme.of(context).colorScheme.surfaceContainerHighest,
              ),
            ),
            Positioned(
              top: 8,
              left: 8,
              child: Container(
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(color: Colors.white, width: 2),
                ),
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(18),
                  child: CachedNetworkImage(
                    imageUrl: imageUrl,
                    width: 36,
                    height: 36,
                    fit: BoxFit.cover,
                    placeholder: (_, _) => Container(
                      color: Theme.of(context).colorScheme.surfaceContainerHighest,
                    ),
                    errorWidget: (_, _, _) => Container(
                      color: Theme.of(context).colorScheme.surfaceContainerHighest,
                      child: const Icon(Icons.store, size: 18),
                    ),
                  ),
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
                      size: 16,
                    ),
                  ),
                ),
              ),
            if (!isOpen)
              Positioned.fill(
                child: Container(
                  color: Colors.black.withAlpha(128),
                  child: Center(
                      child: Text(
                        AppLocalizations.of(context)!.closed,
                        style: AppTextStyles.labelLarge.copyWith(color: Colors.white),
                    ),
                  ),
                ),
              ),
            Positioned(
              bottom: 8,
              right: 8,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: isOpen ? AppColors.success : AppColors.grey500,
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(
                  isOpen ? AppLocalizations.of(context)!.open : AppLocalizations.of(context)!.closed,
                  style: AppTextStyles.caption.copyWith(
                    color: Colors.white,
                    fontWeight: FontWeight.w600,
                    fontSize: 10,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildInfo(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            name,
            style: AppTextStyles.titleSmall,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
          const SizedBox(height: 2),
          Text(
            category,
            style: AppTextStyles.caption.copyWith(fontSize: 11),
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
          ),
          const SizedBox(height: 6),
          Row(
            children: [
              const Icon(Icons.star_rounded, size: 14, color: AppColors.secondary),
              const SizedBox(width: 2),
              Text(
                rating.toStringAsFixed(1),
                style: AppTextStyles.caption.copyWith(
                  fontWeight: FontWeight.w600,
                ),
              ),
              Text(
                ' ($reviewCount)',
                style: AppTextStyles.caption.copyWith(fontSize: 10),
              ),
              const Spacer(),
              const Icon(Icons.location_on_outlined, size: 12, color: AppColors.grey500),
              const SizedBox(width: 2),
              Text(
                Helpers.formatDistance(distance),
                style: AppTextStyles.caption.copyWith(fontSize: 10),
              ),
            ],
          ),
          const SizedBox(height: 4),
          Row(
            children: [
              const Icon(Icons.local_offer_outlined, size: 12, color: AppColors.primary),
              const SizedBox(width: 2),
              Text(
                AppLocalizations.of(context)!.offers(activeOffersCount),
                style: AppTextStyles.caption.copyWith(
                  color: AppColors.primary,
                  fontWeight: FontWeight.w600,
                  fontSize: 10,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
