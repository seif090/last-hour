import 'package:flutter/material.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';

class MerchantOffersScreen extends StatelessWidget {
  const MerchantOffersScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Offers'),
        centerTitle: true,
        actions: [
          IconButton(
            icon: const Icon(Icons.add_rounded),
            onPressed: () {},
            color: AppColors.secondary,
          ),
        ],
      ),
      body: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: 5,
        itemBuilder: (_, i) => _buildOfferCard(theme, i),
      ),
    );
  }

  Widget _buildOfferCard(ThemeData theme, int index) {
    final names = ['Mixed Sushi Box', 'Artisan Croissants', 'Chocolate Cake', 'Fresh Salad Bowl', 'Margherita Pizza'];
    final prices = ['\$9.99', '\$6.50', '\$3.99', '\$4.99', '\$5.99'];
    final remainings = [3, 5, 2, 8, 4];
    final isActive = index < 3;

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: theme.cardColor,
        borderRadius: BorderRadius.circular(14),
        boxShadow: [
          BoxShadow(color: Colors.black.withAlpha(8), blurRadius: 6, offset: const Offset(0, 2)),
        ],
      ),
      child: Row(
        children: [
          Container(
            width: 72,
            height: 72,
            decoration: BoxDecoration(
              color: AppColors.grey200,
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Icon(Icons.image_outlined, color: AppColors.grey400),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(names[index], style: AppTextStyles.titleSmall.copyWith(fontWeight: FontWeight.bold)),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: isActive ? AppColors.success.withAlpha(26) : AppColors.grey100,
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: Text(
                        isActive ? 'Active' : 'Inactive',
                        style: AppTextStyles.caption.copyWith(
                          color: isActive ? AppColors.success : AppColors.grey500,
                          fontWeight: FontWeight.w600,
                          fontSize: 10,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                Text('Price: ${prices[index]}', style: AppTextStyles.caption),
                const SizedBox(height: 2),
                Text('Remaining: ${remainings[index]}', style: AppTextStyles.caption),
                const SizedBox(height: 8),
                _buildProgressBar(remainings[index], 10, theme),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildProgressBar(int remaining, int total, ThemeData theme) {
    final fraction = remaining / total;
    return ClipRRect(
      borderRadius: BorderRadius.circular(4),
      child: LinearProgressIndicator(
        value: fraction,
        minHeight: 6,
        backgroundColor: AppColors.grey200,
        valueColor: AlwaysStoppedAnimation<Color>(
          fraction > 0.5 ? AppColors.success : fraction > 0.2 ? AppColors.warning : AppColors.error,
        ),
      ),
    );
  }
}
