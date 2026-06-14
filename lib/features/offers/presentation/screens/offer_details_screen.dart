import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/countdown_timer.dart';
import '../../../../shared/widgets/custom_button.dart';
import '../../../../shared/widgets/quantity_selector.dart';
import '../../../../shared/widgets/rating_display.dart';
import '../../../../shared/widgets/error_widget_view.dart';
import '../../../../shared/widgets/loading_skeleton.dart';
import '../../domain/entities/offer.dart';
import '../providers/offer_providers.dart';
import '../../../cart/presentation/providers/cart_providers.dart';

class OfferDetailsScreen extends ConsumerStatefulWidget {
  final String? offerId;
  const OfferDetailsScreen({super.key, this.offerId});

  @override
  ConsumerState<OfferDetailsScreen> createState() => _OfferDetailsScreenState();
}

class _OfferDetailsScreenState extends ConsumerState<OfferDetailsScreen> {
  int _quantity = 1;
  int _currentImageIndex = 0;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final offerId = widget.offerId ?? '';
    final offerAsync = ref.watch(offerDetailsProvider(offerId));

    return offerAsync.when(
      data: (offer) => _buildContent(theme, offer),
      loading: () => const Scaffold(body: LoadingSkeleton(itemCount: 1)),
      error: (error, _) => Scaffold(
        body: ErrorWidgetView(
          title: 'Failed to load offer',
          subtitle: error.toString(),
          onRetry: () => ref.invalidate(offerDetailsProvider(offerId)),
        ),
      ),
    );
  }

  Widget _buildContent(ThemeData theme, Offer offer) {
    return Scaffold(
      body: CustomScrollView(
        slivers: [
          _buildImageSlider(theme, offer),
          SliverToBoxAdapter(
            child: _buildDetails(context, theme, offer),
          ),
        ],
      ),
      bottomNavigationBar: _buildBottomBar(theme, offer),
    );
  }

  Widget _buildImageSlider(ThemeData theme, Offer offer) {
    final images = offer.imageUrls.isNotEmpty
        ? offer.imageUrls
        : (offer.imageUrl != null ? [offer.imageUrl!] : [
            'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=600',
          ]);

    return SliverAppBar(
      expandedHeight: 320,
      pinned: true,
      backgroundColor: theme.colorScheme.surface,
      leading: IconButton(
        icon: Container(
          padding: const EdgeInsets.all(8),
          decoration: const BoxDecoration(
            color: Colors.black26,
            shape: BoxShape.circle,
          ),
          child: const Icon(Icons.arrow_back_rounded, color: Colors.white, size: 22),
        ),
        onPressed: () => context.pop(),
      ),
      flexibleSpace: FlexibleSpaceBar(
        background: Stack(
          fit: StackFit.expand,
          children: [
            PageView.builder(
              itemCount: images.length,
              onPageChanged: (index) => setState(() => _currentImageIndex = index),
              itemBuilder: (_, index) => Image.network(
                images[index],
                fit: BoxFit.cover,
                errorBuilder: (_, __, ___) => Container(
                  color: AppColors.grey200,
                  child: Icon(Icons.image_rounded, size: 80, color: AppColors.grey400),
                ),
              ),
            ),
            Positioned(
              bottom: 16,
              right: 16,
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                decoration: BoxDecoration(
                  color: Colors.black54,
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  '${_currentImageIndex + 1}/${images.length}',
                  style: const TextStyle(color: Colors.white, fontSize: 13),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildDetails(BuildContext context, ThemeData theme, Offer offer) {
    final discountPercent = ((offer.originalPrice - offer.discountPrice) / offer.originalPrice * 100).round();

    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.discountRed,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  '-$discountPercent%',
                  style: const TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.bold,
                    fontSize: 13,
                  ),
                ),
              ),
              const SizedBox(width: 10),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.primary.withAlpha(26),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  offer.category,
                  style: TextStyle(
                    color: AppColors.primary,
                    fontWeight: FontWeight.w600,
                    fontSize: 13,
                  ),
                ),
              ),
              const Spacer(),
              IconButton(
                icon: Icon(
                  offer.isFavorite ? Icons.favorite_rounded : Icons.favorite_outline_rounded,
                  color: offer.isFavorite ? AppColors.discountRed : AppColors.grey400,
                ),
                onPressed: () {
                  ref.read(offerRepositoryProvider).toggleFavorite(offer.id);
                },
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            offer.title,
            style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Icon(Icons.store_rounded, size: 16, color: AppColors.grey500),
              const SizedBox(width: 6),
              Text(
                offer.storeName,
                style: AppTextStyles.bodyMedium.copyWith(color: AppColors.grey600),
              ),
              const SizedBox(width: 16),
              const Icon(Icons.location_on_rounded, size: 16, color: AppColors.grey500),
              const SizedBox(width: 4),
              Text(
                '${offer.distance.toStringAsFixed(1)} km',
                style: AppTextStyles.caption.copyWith(color: AppColors.grey500),
              ),
            ],
          ),
          const SizedBox(height: 8),
          RatingDisplay(rating: offer.rating, reviewCount: offer.reviewCount),
          const SizedBox(height: 16),
          const Divider(),
          const SizedBox(height: 16),
          Text(
            'Description',
            style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 8),
          Text(
            offer.description.isNotEmpty ? offer.description : 'No description available.',
            style: AppTextStyles.bodyMedium.copyWith(color: AppColors.grey600, height: 1.5),
          ),
          const SizedBox(height: 20),
          Row(
            children: [
              CountdownTimer(expiryTime: offer.expiryTime),
              const Spacer(),
              Text(
                '${offer.remainingQuantity} left',
                style: AppTextStyles.bodyMedium.copyWith(
                  color: offer.remainingQuantity <= 3 ? AppColors.discountRed : AppColors.grey600,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildBottomBar(ThemeData theme, Offer offer) {
    return Container(
      padding: EdgeInsets.fromLTRB(16, 12, 16, MediaQuery.of(context).padding.bottom + 12),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha(13),
            blurRadius: 10,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: SafeArea(
        top: false,
        child: Row(
          children: [
            QuantitySelector(
              quantity: _quantity,
              maxQuantity: offer.remainingQuantity,
              onChanged: (val) => setState(() => _quantity = val),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: CustomButton(
                label: 'Add to Cart - \$${(offer.discountPrice * _quantity).toStringAsFixed(2)}',
                onPressed: () {
                  ref.read(cartRepositoryProvider).addToCart(
                    offerId: offer.id,
                    quantity: _quantity,
                  );
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Added to cart!')),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
