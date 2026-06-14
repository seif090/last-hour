import 'package:flutter/material.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';

class MerchantReportsScreen extends StatelessWidget {
  const MerchantReportsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Reports'),
        centerTitle: true,
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          _buildPeriodSelector(),
          const SizedBox(height: 20),
          _buildRevenueCard(theme),
          const SizedBox(height: 16),
          _buildStatsGrid(theme),
          const SizedBox(height: 16),
          _buildTopSellingCard(theme),
          const SizedBox(height: 16),
          _buildRecentTransactions(theme),
        ],
      ),
    );
  }

  Widget _buildPeriodSelector() {
    final periods = ['Today', 'This Week', 'This Month'];
    return Container(
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: AppColors.grey100,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: periods.map((p) {
          final isActive = p == 'This Week';
          return Expanded(
            child: GestureDetector(
              onTap: () {},
              child: Container(
                padding: const EdgeInsets.symmetric(vertical: 10),
                decoration: BoxDecoration(
                  color: isActive ? Colors.white : Colors.transparent,
                  borderRadius: BorderRadius.circular(10),
                  boxShadow: isActive
                      ? [BoxShadow(color: Colors.black.withAlpha(13), blurRadius: 4, offset: const Offset(0, 2))]
                      : null,
                ),
                child: Text(
                  p,
                  textAlign: TextAlign.center,
                  style: AppTextStyles.labelMedium.copyWith(
                    fontWeight: FontWeight.w600,
                    color: isActive ? AppColors.primary : AppColors.grey500,
                  ),
                ),
              ),
            ),
          );
        }).toList(),
      ),
    );
  }

  Widget _buildRevenueCard(ThemeData theme) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          colors: [AppColors.primary, AppColors.primaryDark],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Total Revenue', style: AppTextStyles.caption.copyWith(color: Colors.white70)),
          const SizedBox(height: 8),
          Text('\$1,284.50', style: theme.textTheme.headlineMedium?.copyWith(color: Colors.white, fontWeight: FontWeight.bold)),
          const SizedBox(height: 4),
          Row(
            children: [
              Icon(Icons.trending_up, color: Colors.greenAccent, size: 18),
              const SizedBox(width: 4),
              Text('+12.5% vs last week', style: AppTextStyles.caption.copyWith(color: Colors.greenAccent)),
            ],
          ),
          const SizedBox(height: 20),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              _buildRevenueStat('Orders', '48'),
              _buildRevenueStat('Avg. Order', '\$26.76'),
              _buildRevenueStat('Items Sold', '124'),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildRevenueStat(String label, String value) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(value, style: AppTextStyles.titleSmall.copyWith(color: Colors.white, fontWeight: FontWeight.bold)),
        Text(label, style: AppTextStyles.caption.copyWith(color: Colors.white70, fontSize: 10)),
      ],
    );
  }

  Widget _buildStatsGrid(ThemeData theme) {
    return Row(
      children: [
        Expanded(child: _buildStatCard(theme, 'Best Selling', 'Sushi Box', Icons.emoji_events_rounded, AppColors.secondary)),
        const SizedBox(width: 12),
        Expanded(child: _buildStatCard(theme, 'Peak Time', '12-2 PM', Icons.access_time_rounded, AppColors.info)),
      ],
    );
  }

  Widget _buildStatCard(ThemeData theme, String label, String value, IconData icon, Color color) {
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
          Container(
            width: 40, height: 40,
            decoration: BoxDecoration(color: color.withAlpha(26), borderRadius: BorderRadius.circular(10)),
            child: Icon(icon, color: color, size: 22),
          ),
          const SizedBox(height: 12),
          Text(value, style: AppTextStyles.titleSmall.copyWith(fontWeight: FontWeight.bold)),
          Text(label, style: AppTextStyles.caption.copyWith(color: AppColors.grey500)),
        ],
      ),
    );
  }

  Widget _buildTopSellingCard(ThemeData theme) {
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
          Text('Top Selling Items', style: AppTextStyles.titleSmall.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 12),
          _buildTopItem('1', 'Mixed Sushi Box', '32 sold', '\$319.68'),
          const Divider(height: 1),
          _buildTopItem('2', 'Artisan Croissants', '28 sold', '\$182.00'),
          const Divider(height: 1),
          _buildTopItem('3', 'Chocolate Cake', '21 sold', '\$83.79'),
        ],
      ),
    );
  }

  Widget _buildTopItem(String rank, String name, String sold, String revenue) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        children: [
          Container(
            width: 28, height: 28,
            decoration: BoxDecoration(
              color: rank == '1' ? AppColors.secondary.withAlpha(26) : AppColors.grey100,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Center(
              child: Text(rank, style: AppTextStyles.labelMedium.copyWith(
                fontWeight: FontWeight.bold,
                color: rank == '1' ? AppColors.secondary : AppColors.grey500,
              )),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(name, style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                Text(sold, style: AppTextStyles.caption),
              ],
            ),
          ),
          Text(revenue, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.bold, color: AppColors.primary)),
        ],
      ),
    );
  }

  Widget _buildRecentTransactions(ThemeData theme) {
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
          Text('Recent Transactions', style: AppTextStyles.titleSmall.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 12),
          _buildTransactionRow('Online Payment', '\$9.99', '2 min ago', AppColors.success),
          const Divider(height: 1),
          _buildTransactionRow('Online Payment', '\$6.50', '15 min ago', AppColors.success),
          const Divider(height: 1),
          _buildTransactionRow('Cash on Pickup', '\$3.99', '32 min ago', AppColors.warning),
        ],
      ),
    );
  }

  Widget _buildTransactionRow(String method, String amount, String time, Color statusColor) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        children: [
          Container(
            width: 40, height: 40,
            decoration: BoxDecoration(color: statusColor.withAlpha(26), borderRadius: BorderRadius.circular(10)),
            child: Icon(method.contains('Cash') ? Icons.money_rounded : Icons.credit_card_rounded, color: statusColor, size: 20),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(method, style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                Text(time, style: AppTextStyles.caption),
              ],
            ),
          ),
          Text(amount, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.bold, color: AppColors.primary)),
        ],
      ),
    );
  }
}
