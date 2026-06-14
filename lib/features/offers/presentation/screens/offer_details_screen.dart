import 'package:flutter/material.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/countdown_timer.dart';
import '../../../../shared/widgets/custom_button.dart';
import '../../../../shared/widgets/quantity_selector.dart';
import '../../../../shared/widgets/rating_display.dart';

class OfferDetailsScreen extends StatefulWidget {
  const OfferDetailsScreen({super.key});

  @override
  State<OfferDetailsScreen> createState() => _OfferDetailsScreenState();
}

class _OfferDetailsScreenState extends State<OfferDetailsScreen> {
  int _quantity = 1;
  int _currentImageIndex = 0;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final expiryTime = DateTime.now().add(const Duration(hours: 2, minutes: 30));

    return Scaffold(
      body: CustomScrollView(
        slivers: [
          _buildImageSlider(theme),
          SliverToBoxAdapter(
            child: _buildContent(context, theme, expiryTime),
          ),
        ],
      ),
      bottomNavigationBar: _buildBottomBar(theme, expiryTime),
    );
  }

  Widget _buildImageSlider(ThemeData theme) {
    final images = [
      'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=600',
      'https://images.unsplash.com/photo-1553621042-f6e147245754?w=600',
      'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=600',
    ];

    return SliverAppBar(
      expandedHeight: 300,
      pinned: true,
      backgroundColor: theme.scaffoldBackgroundColor,
      surfaceTintColor: Colors.transparent,
      leading: Padding(
        padding: const EdgeInsets.all(8),
        child: Material(
          color: Colors.black.withAlpha(102),
          borderRadius: BorderRadius.circular(12),
          child: InkWell(
            onTap: () => Navigator.of(context).pop(),
            borderRadius: BorderRadius.circular(12),
            child: const Icon(Icons.arrow_back_rounded, color: Colors.white),
          ),
        ),
      ),
      actions: [
        Padding(
          padding: const EdgeInsets.all(8),
          child: Material(
            color: Colors.black.withAlpha(102),
            borderRadius: BorderRadius.circular(12),
            child: InkWell(
              onTap: () {},
              borderRadius: BorderRadius.circular(12),
              child: const Padding(
                padding: EdgeInsets.all(10),
                child: Icon(Icons.favorite_border, color: Colors.white, size: 20),
              ),
            ),
          ),
        ),
        Padding(
          padding: const EdgeInsets.all(8),
          child: Material(
            color: Colors.black.withAlpha(102),
            borderRadius: BorderRadius.circular(12),
            child: InkWell(
              onTap: () {},
              borderRadius: BorderRadius.circular(12),
              child: const Padding(
                padding: EdgeInsets.all(10),
                child: Icon(Icons.share_outlined, color: Colors.white, size: 20),
              ),
            ),
          ),
        ),
      ],
      flexibleSpace: FlexibleSpaceBar(
        background: Stack(
          children: [
            PageView.builder(
              itemCount: images.length,
              onPageChanged: (i) => setState(() => _currentImageIndex = i),
              itemBuilder: (_, i) => Container(
                color: AppColors.grey200,
                child: Center(
                  child: Icon(Icons.image_outlined, size: 80, color: AppColors.grey400),
                ),
              ),
            ),
            Positioned(
              bottom: 16,
              left: 0,
              right: 0,
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: List.generate(
                  images.length,
                  (i) => AnimatedContainer(
                    duration: const Duration(milliseconds: 300),
                    margin: const EdgeInsets.symmetric(horizontal: 3),
                    width: _currentImageIndex == i ? 20 : 8,
                    height: 8,
                    decoration: BoxDecoration(
                      color: _currentImageIndex == i
                          ? Colors.white
                          : Colors.white.withAlpha(102),
                      borderRadius: BorderRadius.circular(4),
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

  Widget _buildContent(BuildContext context, ThemeData theme, DateTime expiryTime) {
    return Padding(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                decoration: BoxDecoration(
                  color: AppColors.discountRed.withAlpha(26),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  '-60%',
                  style: AppTextStyles.labelLarge.copyWith(
                    color: AppColors.discountRed,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
              const SizedBox(width: 10),
              CountdownTimer(
                expiryTime: expiryTime,
                textStyle: AppTextStyles.labelLarge.copyWith(
                  color: AppColors.primary,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Text(
            'Mixed Sushi Box',
            style: theme.textTheme.headlineSmall?.copyWith(
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              const RatingDisplay(rating: 4.8, reviewCount: 124),
              const Spacer(),
              Text(
                '1.2 km away',
                style: AppTextStyles.caption.copyWith(color: AppColors.grey500),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              Text(
                '\$24.99',
                style: AppTextStyles.titleLarge.copyWith(
                  color: AppColors.grey400,
                  decoration: TextDecoration.lineThrough,
                ),
              ),
              const SizedBox(width: 10),
              Text(
                '\$9.99',
                style: AppTextStyles.headlineMedium.copyWith(
                  color: AppColors.primary,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          const SizedBox(height: 24),
          const Divider(),
          const SizedBox(height: 16),
          _buildStoreInfo(theme),
          const SizedBox(height: 24),
          const Divider(),
          const SizedBox(height: 16),
          Text(
            'Description',
            style: theme.textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 8),
          Text(
            'Freshly prepared mixed sushi box with salmon, tuna, and vegetable rolls. Perfect for a quick lunch or dinner. Includes soy sauce, wasabi, and pickled ginger.',
            style: AppTextStyles.bodyMedium.copyWith(
              color: AppColors.grey600,
              height: 1.6,
            ),
          ),
          const SizedBox(height: 24),
          const Divider(),
          const SizedBox(height: 16),
          Text(
            'Pickup Time',
            style: theme.textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              const Icon(Icons.access_time_rounded, size: 20, color: AppColors.primary),
              const SizedBox(width: 8),
              Text(
                'Today, 6:00 PM - 9:00 PM',
                style: AppTextStyles.bodyMedium,
              ),
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              const Icon(Icons.location_on_rounded, size: 20, color: AppColors.primary),
              const SizedBox(width: 8),
              Text(
                '123 Main Street, Downtown',
                style: AppTextStyles.bodyMedium,
              ),
            ],
          ),
          const SizedBox(height: 24),
          const Divider(),
          const SizedBox(height: 16),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Quantity Available',
                style: theme.textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              Text(
                '3 remaining',
                style: AppTextStyles.labelLarge.copyWith(
                  color: AppColors.discountRed,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          const SizedBox(height: 30),
        ],
      ),
    );
  }

  Widget _buildStoreInfo(ThemeData theme) {
    return Row(
      children: [
        Container(
          width: 56,
          height: 56,
          decoration: BoxDecoration(
            color: AppColors.primary.withAlpha(26),
            borderRadius: BorderRadius.circular(14),
          ),
          child: const Icon(Icons.store_rounded, color: AppColors.primary, size: 28),
        ),
        const SizedBox(width: 14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Sakura Japanese',
                style: AppTextStyles.titleMedium.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                'Japanese Restaurant',
                style: AppTextStyles.caption,
              ),
            ],
          ),
        ),
        TextButton(
          onPressed: () {},
          child: const Text('View Store'),
        ),
      ],
    );
  }

  Widget _buildBottomBar(ThemeData theme, DateTime expiryTime) {
    return Container(
      padding: const EdgeInsets.fromLTRB(20, 12, 20, 24),
      decoration: BoxDecoration(
        color: theme.scaffoldBackgroundColor,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha(26),
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
              minQuantity: 1,
              maxQuantity: 3,
              onChanged: (q) => setState(() => _quantity = q),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: CustomButton(
                label: 'Add to Cart - \$${(_quantity * 9.99).toStringAsFixed(2)}',
                onPressed: () {},
              ),
            ),
          ],
        ),
      ),
    );
  }
}
