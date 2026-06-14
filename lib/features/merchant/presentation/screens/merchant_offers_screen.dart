import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/empty_state_widget.dart';
import '../../../offers/domain/entities/offer.dart';
import '../providers/merchant_providers.dart';

class MerchantOffersScreen extends ConsumerWidget {
  const MerchantOffersScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final offersAsync = ref.watch(merchantOffersProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Offers'),
        centerTitle: true,
        actions: [
          IconButton(
            icon: const Icon(Icons.add_rounded),
            onPressed: () => context.push('/merchant/offers/create'),
            color: AppColors.secondary,
          ),
        ],
      ),
      body: offersAsync.when(
        data: (offers) {
          if (offers.isEmpty) return const EmptyStateWidget.offers();
          return ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: offers.length,
            itemBuilder: (_, i) => _buildOfferCard(theme, offers[i], ref),
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, __) => Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Text('Failed to load offers'),
              const SizedBox(height: 8),
              TextButton(onPressed: () => ref.invalidate(merchantOffersProvider), child: const Text('Retry')),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildOfferCard(ThemeData theme, Offer offer, WidgetRef ref) {
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: theme.cardColor,
        borderRadius: BorderRadius.circular(14),
        boxShadow: [BoxShadow(color: Colors.black.withAlpha(8), blurRadius: 6, offset: const Offset(0, 2))],
      ),
      child: Row(
        children: [
          Container(
            width: 64, height: 64,
            decoration: BoxDecoration(
              color: AppColors.grey200,
              borderRadius: BorderRadius.circular(12),
            ),
            child: offer.imageUrl != null
                ? ClipRRect(borderRadius: BorderRadius.circular(12), child: Image.network(offer.imageUrl!, fit: BoxFit.cover, errorBuilder: (_, __, ___) => Icon(Icons.image_rounded, color: AppColors.grey400)))
                : const Icon(Icons.image_rounded, color: AppColors.grey400),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(child: Text(offer.title, style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600))),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: offer.isActive ? AppColors.success.withAlpha(20) : AppColors.grey200,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(
                        offer.isActive ? 'Active' : 'Inactive',
                        style: AppTextStyles.caption.copyWith(
                          color: offer.isActive ? AppColors.success : AppColors.grey500,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                Text('\$${offer.discountPrice.toStringAsFixed(2)} · ${offer.remainingQuantity}/${offer.originalQuantity} left',
                  style: AppTextStyles.caption.copyWith(color: AppColors.grey500)),
                const SizedBox(height: 6),
                ClipRRect(
                  borderRadius: BorderRadius.circular(4),
                  child: LinearProgressIndicator(
                    value: offer.originalQuantity > 0 ? offer.remainingQuantity / offer.originalQuantity : 0,
                    backgroundColor: AppColors.grey200,
                    valueColor: AlwaysStoppedAnimation<Color>(
                      offer.remainingQuantity <= 3 ? AppColors.discountRed : AppColors.primary,
                    ),
                    minHeight: 4,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
