import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/loading_skeleton.dart';
import '../../../../shared/widgets/error_widget_view.dart';
import '../providers/merchant_providers.dart';

class MerchantReportsScreen extends ConsumerWidget {
  const MerchantReportsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final reportsAsync = ref.watch(merchantReportsProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Reports'),
        centerTitle: true,
      ),
      body: reportsAsync.when(
        data: (data) => ListView(
          padding: const EdgeInsets.all(16),
          children: [
            _buildPeriodSelector(),
            const SizedBox(height: 20),
            _buildRevenueCard(theme, data),
            const SizedBox(height: 16),
            _buildStatsGrid(theme, data),
            const SizedBox(height: 16),
            _buildTopSellingCard(theme, data),
            const SizedBox(height: 16),
            _buildRecentTransactions(theme, data),
          ],
        ),
      loading: () => const Scaffold(body: LoadingSkeleton(itemCount: 3)),
      error: (error, _) => Scaffold(
        body: ErrorWidgetView(
          title: 'Failed to load reports',
          subtitle: error.toString(),
          onRetry: () => ref.invalidate(merchantReportsProvider),
        ),
      ),
      ),
    );
  }

  Widget _buildPeriodSelector() {
    return Container(
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: AppColors.grey100,
        borderRadius: BorderRadius.circular(12),
      ),
      child: const Row(
        children: [
          _PeriodChip(label: 'Today', isActive: false),
          _PeriodChip(label: 'This Week', isActive: true),
          _PeriodChip(label: 'This Month', isActive: false),
        ],
      ),
    );
  }

  Widget _buildRevenueCard(ThemeData theme, Map<String, dynamic> data) {
    final revenue = data['total_revenue'] ?? 0.0;
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: const LinearGradient(colors: [AppColors.secondary, AppColors.secondaryDark]),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Total Revenue', style: AppTextStyles.bodyMedium.copyWith(color: Colors.white70)),
          const SizedBox(height: 8),
          Text('\$${(revenue is num ? revenue : 0).toStringAsFixed(2)}',
            style: theme.textTheme.headlineLarge?.copyWith(color: Colors.white, fontWeight: FontWeight.bold)),
          const SizedBox(height: 4),
          Text('${data['total_orders'] ?? 0} orders completed',
            style: AppTextStyles.caption.copyWith(color: Colors.white60)),
        ],
      ),
    );
  }

  Widget _buildStatsGrid(ThemeData theme, Map<String, dynamic> data) {
    final stats = [
      {'label': 'Avg. Order', 'value': '\$${(data['avg_order_value'] ?? 0.0).toStringAsFixed(2)}', 'icon': Icons.shopping_cart_rounded},
      {'label': 'Items Saved', 'value': '${data['items_saved'] ?? 0}', 'icon': Icons.eco_rounded},
      {'label': 'Rating', 'value': (data['rating'] ?? 0.0).toStringAsFixed(1), 'icon': Icons.star_rounded},
      {'label': 'Customers', 'value': '${data['customers'] ?? 0}', 'icon': Icons.people_rounded},
    ];

    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2,
        childAspectRatio: 1.6,
        crossAxisSpacing: 12,
        mainAxisSpacing: 12,
      ),
      itemCount: stats.length,
      itemBuilder: (_, i) => Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: theme.cardColor,
          borderRadius: BorderRadius.circular(14),
          boxShadow: [BoxShadow(color: Colors.black.withAlpha(8), blurRadius: 6, offset: const Offset(0, 2))],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(stats[i]['icon'] as IconData, color: AppColors.secondary, size: 22),
            const Spacer(),
            Text(stats[i]['value'] as String, style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
            Text(stats[i]['label'] as String, style: AppTextStyles.caption.copyWith(color: AppColors.grey500)),
          ],
        ),
      ),
    );
  }

  Widget _buildTopSellingCard(ThemeData theme, Map<String, dynamic> data) {
    final topSelling = data['top_selling'] as List<dynamic>? ?? [];
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.cardColor,
        borderRadius: BorderRadius.circular(14),
        boxShadow: [BoxShadow(color: Colors.black.withAlpha(8), blurRadius: 6, offset: const Offset(0, 2))],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Top Selling Items', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: 12),
          if (topSelling.isEmpty)
            const Padding(padding: EdgeInsets.all(16), child: Center(child: Text('No data yet', style: TextStyle(color: AppColors.grey500)))),
          ...topSelling.take(5).map((item) {
            final map = item as Map<String, dynamic>;
            return Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: Row(
                children: [
                  Container(
                    width: 36, height: 36,
                    decoration: BoxDecoration(color: AppColors.secondary.withAlpha(26), borderRadius: BorderRadius.circular(10)),
                    child: const Icon(Icons.shopping_bag_rounded, color: AppColors.secondary, size: 18),
                  ),
                  const SizedBox(width: 12),
                  Expanded(child: Text(map['name'] ?? 'Item', style: AppTextStyles.bodyMedium)),
                  Text('${map['count'] ?? 0} sold', style: AppTextStyles.caption.copyWith(color: AppColors.grey500)),
                  const SizedBox(width: 12),
                  Text('\$${(map['revenue'] ?? 0).toStringAsFixed(2)}', style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                ],
              ),
            );
          }),
        ],
      ),
    );
  }

  Widget _buildRecentTransactions(ThemeData theme, Map<String, dynamic> data) {
    final transactions = data['transactions'] as List<dynamic>? ?? [];
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.cardColor,
        borderRadius: BorderRadius.circular(14),
        boxShadow: [BoxShadow(color: Colors.black.withAlpha(8), blurRadius: 6, offset: const Offset(0, 2))],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Recent Transactions', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: 12),
          if (transactions.isEmpty)
            const Padding(padding: EdgeInsets.all(16), child: Center(child: Text('No transactions yet', style: TextStyle(color: AppColors.grey500)))),
          ...transactions.take(5).map((txn) {
            final map = txn as Map<String, dynamic>;
            return Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: Row(
                children: [
                  Container(
                    width: 36, height: 36,
                    decoration: BoxDecoration(
                      color: (map['type'] == 'refund' ? AppColors.discountRed : AppColors.success).withAlpha(26),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Icon(
                      map['type'] == 'refund' ? Icons.arrow_downward_rounded : Icons.arrow_upward_rounded,
                      color: map['type'] == 'refund' ? AppColors.discountRed : AppColors.success,
                      size: 18,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(map['description'] ?? 'Transaction', style: AppTextStyles.bodyMedium),
                        Text(map['date'] ?? '', style: AppTextStyles.caption.copyWith(color: AppColors.grey500)),
                      ],
                    ),
                  ),
                  Text(
                    '${map['type'] == 'refund' ? '-' : '+'}\$${(map['amount'] ?? 0).toStringAsFixed(2)}',
                    style: AppTextStyles.bodyMedium.copyWith(
                      fontWeight: FontWeight.w600,
                      color: map['type'] == 'refund' ? AppColors.discountRed : AppColors.success,
                    ),
                  ),
                ],
              ),
            );
          }),
        ],
      ),
    );
  }
}

class _PeriodChip extends StatelessWidget {
  final String label;
  final bool isActive;
  const _PeriodChip({required this.label, required this.isActive});

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: GestureDetector(
        onTap: () {},
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: 10),
          decoration: BoxDecoration(
            color: isActive ? Colors.white : Colors.transparent,
            borderRadius: BorderRadius.circular(10),
          ),
          child: Text(
            label,
            textAlign: TextAlign.center,
            style: AppTextStyles.labelMedium.copyWith(
              fontWeight: FontWeight.w600,
              color: isActive ? AppColors.secondary : AppColors.grey600,
            ),
          ),
        ),
      ),
    );
  }
}
