import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';

class MerchantDashboardScreen extends StatelessWidget {
  const MerchantDashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            _buildHeader(theme),
            const SizedBox(height: 20),
            _buildKpiRow(theme),
            const SizedBox(height: 24),
            _buildQuickActions(theme, context),
            const SizedBox(height: 24),
            _buildRecentOrders(theme, context),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(ThemeData theme) {
    return Row(
      children: [
        Container(
          width: 48,
          height: 48,
          decoration: BoxDecoration(
            color: AppColors.secondary,
            borderRadius: BorderRadius.circular(12),
          ),
          child: const Icon(Icons.store_rounded, color: Colors.white),
        ),
        const SizedBox(width: 14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Le Pain Bakery', style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
              Text('Online · 5 active offers', style: AppTextStyles.caption.copyWith(color: AppColors.success)),
            ],
          ),
        ),
        IconButton(
          icon: const Icon(Icons.notifications_outlined),
          onPressed: () {},
          color: AppColors.grey600,
        ),
      ],
    );
  }

  Widget _buildKpiRow(ThemeData theme) {
    return Row(
      children: [
        Expanded(child: _buildKpiCard('Active Offers', '5', Icons.local_offer_rounded, AppColors.primary, theme)),
        const SizedBox(width: 12),
        Expanded(child: _buildKpiCard('Today\'s Orders', '12', Icons.receipt_long_rounded, AppColors.secondary, theme)),
      ],
    );
  }

  Widget _buildKpiCard(String label, String value, IconData icon, Color color, ThemeData theme) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(color: Colors.black.withAlpha(10), blurRadius: 8, offset: const Offset(0, 2)),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(color: color.withAlpha(26), borderRadius: BorderRadius.circular(10)),
            child: Icon(icon, color: color, size: 22),
          ),
          const SizedBox(height: 14),
          Text(value, style: theme.textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 4),
          Text(label, style: AppTextStyles.caption.copyWith(color: AppColors.grey500)),
        ],
      ),
    );
  }

  Widget _buildQuickActions(ThemeData theme, BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Quick Actions', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(child: _buildActionCard(Icons.add_circle_rounded, 'New Offer', AppColors.primary, context)),
            const SizedBox(width: 12),
            Expanded(child: _buildActionCard(Icons.inventory_2_rounded, 'Manage Offers', AppColors.secondary, context)),
            const SizedBox(width: 12),
            Expanded(child: _buildActionCard(Icons.receipt_long_rounded, 'Orders', AppColors.tertiary, context)),
          ],
        ),
      ],
    );
  }

  Widget _buildActionCard(IconData icon, String label, Color color, BuildContext context) {
    return GestureDetector(
      onTap: () {},
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 20, horizontal: 12),
        decoration: BoxDecoration(
          color: color.withAlpha(15),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: color.withAlpha(51)),
        ),
        child: Column(
          children: [
            Icon(icon, color: color, size: 28),
            const SizedBox(height: 8),
            Text(label, style: AppTextStyles.caption.copyWith(color: color, fontWeight: FontWeight.w600), textAlign: TextAlign.center),
          ],
        ),
      ),
    );
  }

  Widget _buildRecentOrders(ThemeData theme, BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text('Recent Orders', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            TextButton(
              onPressed: () => context.go('/merchant/orders'),
              child: const Text('View All', style: TextStyle(color: AppColors.secondary)),
            ),
          ],
        ),
        const SizedBox(height: 8),
        _buildOrderRow('#1024', 'Mixed Sushi Box', '5 min ago', '\$9.99', AppColors.warning),
        const Divider(height: 1),
        _buildOrderRow('#1023', 'Artisan Croissants', '12 min ago', '\$6.50', AppColors.primary),
        const Divider(height: 1),
        _buildOrderRow('#1022', 'Chocolate Cake', '18 min ago', '\$3.99', AppColors.success),
      ],
    );
  }

  Widget _buildOrderRow(String orderId, String item, String time, String amount, Color statusColor) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12),
      child: Row(
        children: [
          Container(
            width: 44,
            height: 44,
            decoration: BoxDecoration(color: AppColors.grey100, borderRadius: BorderRadius.circular(10)),
            child: const Icon(Icons.image_outlined, color: AppColors.grey400, size: 22),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(orderId, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.bold)),
                Text(item, style: AppTextStyles.caption),
              ],
            ),
          ),
          Text(amount, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.bold, color: AppColors.primary)),
          const SizedBox(width: 12),
          Text(time, style: AppTextStyles.caption.copyWith(fontSize: 10)),
        ],
      ),
    );
  }
}
