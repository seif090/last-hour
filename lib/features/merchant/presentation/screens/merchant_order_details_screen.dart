import 'package:flutter/material.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/custom_button.dart';

class MerchantOrderDetailsScreen extends StatelessWidget {
  const MerchantOrderDetailsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Order #1024'),
        centerTitle: true,
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _buildStatusCard(theme),
          const SizedBox(height: 16),
          _buildCustomerCard(theme),
          const SizedBox(height: 16),
          _buildItemsCard(theme),
          const SizedBox(height: 16),
          _buildTimeline(theme),
          const SizedBox(height: 24),
          CustomButton(
            label: 'Mark as Ready',
            onPressed: () {},
            backgroundColor: AppColors.success,
          ),
          const SizedBox(height: 12),
          CustomButton(
            label: 'Contact Customer',
            onPressed: () {},
            type: CustomButtonType.outline,
          ),
          const SizedBox(height: 32),
        ],
      ),
    );
  }

  Widget _buildStatusCard(ThemeData theme) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.warning.withAlpha(20),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.warning.withAlpha(51)),
      ),
      child: Row(
        children: [
          Container(
            width: 48, height: 48,
            decoration: BoxDecoration(
              color: AppColors.warning.withAlpha(51),
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Icon(Icons.hourglass_empty, color: AppColors.warning),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Pending Preparation', style: AppTextStyles.titleSmall.copyWith(fontWeight: FontWeight.bold)),
                Text('Order placed 12 min ago', style: AppTextStyles.caption),
              ],
            ),
          ),
          Text('\$31.94', style: AppTextStyles.titleMedium.copyWith(fontWeight: FontWeight.bold, color: AppColors.primary)),
        ],
      ),
    );
  }

  Widget _buildCustomerCard(ThemeData theme) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [BoxShadow(color: Colors.black.withAlpha(8), blurRadius: 6, offset: const Offset(0, 2))],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Customer', style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.bold, color: AppColors.grey500)),
          const SizedBox(height: 12),
          Row(
            children: [
              Container(
                width: 44, height: 44,
                decoration: BoxDecoration(
                  gradient: AppColors.primaryGradient,
                  shape: BoxShape.circle,
                ),
                child: Center(
                  child: Text('J', style: AppTextStyles.titleSmall.copyWith(color: Colors.white, fontWeight: FontWeight.bold)),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('John Doe', style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.bold)),
                    Text('john@example.com', style: AppTextStyles.caption),
                  ],
                ),
              ),
              Icon(Icons.phone_outlined, color: AppColors.primary, size: 22),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildItemsCard(ThemeData theme) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [BoxShadow(color: Colors.black.withAlpha(8), blurRadius: 6, offset: const Offset(0, 2))],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Items', style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.bold, color: AppColors.grey500)),
          const SizedBox(height: 12),
          _buildItemRow('Mixed Sushi Box', '\$9.99', '2x'),
          const Divider(height: 1),
          _buildItemRow('Artisan Croissants', '\$6.50', '1x'),
          const Divider(height: 1),
          _buildItemRow('Chocolate Cake', '\$3.99', '3x'),
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 10),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Total', style: AppTextStyles.titleSmall.copyWith(fontWeight: FontWeight.bold)),
                Text('\$31.94', style: AppTextStyles.titleSmall.copyWith(fontWeight: FontWeight.bold, color: AppColors.primary)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildItemRow(String name, String price, String qty) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        children: [
          Expanded(child: Text(name, style: AppTextStyles.bodyMedium)),
          Text('$qty  ', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.grey500)),
          Text(price, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }

  Widget _buildTimeline(ThemeData theme) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [BoxShadow(color: Colors.black.withAlpha(8), blurRadius: 6, offset: const Offset(0, 2))],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Timeline', style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.bold, color: AppColors.grey500)),
          const SizedBox(height: 16),
          _buildTimelineStep(Icons.check_circle, 'Order Placed', '12:30 PM', AppColors.success, true),
          _buildTimelineStep(Icons.hourglass_empty, 'Preparing', 'In progress', AppColors.warning, true),
          _buildTimelineStep(Icons.shopping_bag_outlined, 'Ready for Pickup', 'Pending', AppColors.grey300, false),
          _buildTimelineStep(Icons.check_circle_outline, 'Picked Up', 'Pending', AppColors.grey300, false),
        ],
      ),
    );
  }

  Widget _buildTimelineStep(IconData icon, String title, String time, Color color, bool isActive) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Row(
        children: [
          Icon(icon, color: isActive ? color : AppColors.grey300, size: 24),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600, color: isActive ? null : AppColors.grey400)),
                Text(time, style: AppTextStyles.caption.copyWith(color: isActive ? AppColors.grey500 : AppColors.grey300)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
