import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../../core/routes/route_names.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/offer_card.dart';
import '../../../../shared/widgets/section_header.dart';
import '../../data/models/home_models.dart';
import '../providers/home_provider.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final categories = ref.watch(homeCategoriesProvider);
    final featuredOffersAsync = ref.watch(homeFeaturedOffersProvider);
    final filteredOffersAsync = ref.watch(filteredOffersProvider);
    final selectedCategory = ref.watch(selectedCategoryProvider);

    final featuredOffers = featuredOffersAsync.when(
      data: (data) => data,
      loading: () => <HomeOffer>[],
      error: (_, __) => <HomeOffer>[],
    );
    final filteredOffers = filteredOffersAsync.when(
      data: (data) => data,
      loading: () => <HomeOffer>[],
      error: (_, __) => <HomeOffer>[],
    );

    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () => Future.delayed(const Duration(seconds: 1)),
          child: CustomScrollView(
            physics: const BouncingScrollPhysics(),
            slivers: [
              _buildAppBar(context, ref),
              _buildLocationBar(context),
              _buildSearchBar(context),
              _buildCategoriesSection(context, categories, selectedCategory, ref),
              _buildFeaturedSection(context, featuredOffers),
              _buildNearbySection(context, filteredOffers, ref),
              const SliverPadding(padding: EdgeInsets.only(bottom: 24)),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildAppBar(BuildContext context, WidgetRef ref) {
    return SliverAppBar(
      floating: true,
      pinned: false,
      snap: true,
      backgroundColor: Theme.of(context).scaffoldBackgroundColor,
      surfaceTintColor: Colors.transparent,
      title: Row(
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: AppColors.primary,
              borderRadius: BorderRadius.circular(10),
            ),
            child: const Icon(
              Icons.restaurant_menu_rounded,
              color: Colors.white,
              size: 20,
            ),
          ),
          const SizedBox(width: 10),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'DELIVER TO',
                style: AppTextStyles.caption.copyWith(
                  fontSize: 9,
                  letterSpacing: 1.2,
                  fontWeight: FontWeight.w600,
                ),
              ),
              Row(
                children: [
                  const Icon(Icons.location_on_rounded, size: 14, color: AppColors.primary),
                  const SizedBox(width: 4),
                  Text(
                    'Current Location',
                    style: AppTextStyles.labelMedium.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const Icon(Icons.keyboard_arrow_down_rounded, size: 18),
                ],
              ),
            ],
          ),
        ],
      ),
      actions: [
        Stack(
          children: [
            IconButton(
              icon: const Icon(Icons.notifications_outlined),
              onPressed: () {},
              color: AppColors.grey700,
            ),
            Positioned(
              top: 8,
              right: 8,
              child: Container(
                width: 8,
                height: 8,
                decoration: const BoxDecoration(
                  color: AppColors.discountRed,
                  shape: BoxShape.circle,
                ),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildLocationBar(BuildContext context) {
    return SliverToBoxAdapter(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
        child: Row(
          children: [
            Icon(Icons.access_time_rounded, size: 14, color: AppColors.grey500),
            const SizedBox(width: 6),
            Text(
              'Order by 9 PM for pickup',
              style: AppTextStyles.caption.copyWith(
                color: AppColors.grey600,
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSearchBar(BuildContext context) {
    return SliverToBoxAdapter(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
        child: GestureDetector(
          onTap: () => context.push(RouteNames.search),
          child: Container(
            height: 48,
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.surfaceContainerHighest.withAlpha(80),
              borderRadius: BorderRadius.circular(14),
            ),
            child: Row(
              children: [
                const SizedBox(width: 14),
                Icon(Icons.search_rounded, color: AppColors.grey500, size: 22),
                const SizedBox(width: 10),
                Text(
                  'Search offers, stores...',
                  style: AppTextStyles.bodyMedium.copyWith(color: AppColors.grey400),
                ),
                const Spacer(),
                Container(
                  margin: const EdgeInsets.all(6),
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: AppColors.primary.withAlpha(26),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(Icons.tune_rounded, size: 18, color: AppColors.primary),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildCategoriesSection(
    BuildContext context,
    List<Category> categories,
    String? selectedCategory,
    WidgetRef ref,
  ) {
    return SliverToBoxAdapter(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionHeader(title: 'Categories'),
          SizedBox(
            height: 44,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16),
              itemCount: categories.length,
              separatorBuilder: (_, __) => const SizedBox(width: 10),
              itemBuilder: (context, index) {
                final category = categories[index];
                final isSelected = selectedCategory == category.name;
                return GestureDetector(
                  onTap: () {
                    ref.read(selectedCategoryProvider.notifier).state =
                        isSelected ? null : category.name;
                  },
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 10),
                    decoration: BoxDecoration(
                      color: isSelected ? category.color : Colors.transparent,
                      borderRadius: BorderRadius.circular(22),
                      border: Border.all(
                        color: isSelected ? category.color : AppColors.grey300,
                      ),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          category.icon,
                          size: 18,
                          color: isSelected ? Colors.white : category.color,
                        ),
                        const SizedBox(width: 8),
                        Text(
                          category.name,
                          style: AppTextStyles.labelMedium.copyWith(
                            color: isSelected ? Colors.white : AppColors.grey700,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildFeaturedSection(BuildContext context, List<HomeOffer> offers) {
    return SliverToBoxAdapter(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionHeader(title: 'Featured Offers', actionLabel: 'See All'),
          SizedBox(
            height: 320,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16),
              itemCount: offers.length,
              separatorBuilder: (_, __) => const SizedBox(width: 12),
              itemBuilder: (context, index) {
                final offer = offers[index];
                return SizedBox(
                  width: 260,
                  child: OfferCard(
                    id: offer.id,
                    imageUrl: _getPlaceholderImage(index),
                    title: offer.title,
                    storeName: offer.storeName,
                    originalPrice: offer.originalPrice,
                    discountPrice: offer.discountPrice,
                    remainingQuantity: offer.remainingQuantity,
                    expiryTime: offer.expiryTime,
                    distance: offer.distance,
                    rating: offer.rating,
                    onTap: () => context.push(
                      '${RouteNames.offerDetails}/${offer.id}',
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildNearbySection(
    BuildContext context,
    List<HomeOffer> offers,
    WidgetRef ref,
  ) {
    if (offers.isEmpty) {
      return SliverToBoxAdapter(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Center(
            child: Text(
              'No offers in this category nearby',
              style: AppTextStyles.bodyMedium.copyWith(color: AppColors.grey500),
            ),
          ),
        ),
      );
    }

    return SliverToBoxAdapter(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const SectionHeader(title: 'Nearby Offers', actionLabel: 'See All'),
          ...offers.take(4).map((offer) => Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
            child: OfferCard(
              id: offer.id,
              imageUrl: _getPlaceholderImage(offers.indexOf(offer)),
              title: offer.title,
              storeName: offer.storeName,
              originalPrice: offer.originalPrice,
              discountPrice: offer.discountPrice,
              remainingQuantity: offer.remainingQuantity,
              expiryTime: offer.expiryTime,
              distance: offer.distance,
              rating: offer.rating,
              onTap: () => context.push(
                '${RouteNames.offerDetails}/${offer.id}',
              ),
            ),
          )),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: OutlinedButton(
              onPressed: () {},
              style: OutlinedButton.styleFrom(
                minimumSize: const Size(double.infinity, 48),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(14),
                ),
              ),
              child: const Text('View All Offers'),
            ),
          ),
        ],
      ),
    );
  }

  String _getPlaceholderImage(int index) {
    final images = [
      'https://images.unsplash.com/photo-1579871494447-9811cf80d66c?w=400',
      'https://images.unsplash.com/photo-1555507036-ab1f4038028a?w=400',
      'https://images.unsplash.com/photo-1578985545062-69928b1d9587?w=400',
      'https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=400',
      'https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?w=400',
      'https://images.unsplash.com/photo-1610832958506-aa56368176cf?w=400',
    ];
    return images[index % images.length];
  }
}
