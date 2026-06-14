import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/loading_skeleton.dart';
import '../../../../shared/widgets/error_widget_view.dart';
import '../../domain/entities/merchant.dart';
import '../../../orders/domain/entities/order.dart';
import '../providers/merchant_providers.dart';

class MerchantDashboardScreen extends ConsumerWidget {
  const MerchantDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final dashboardAsync = ref.watch(merchantDashboardProvider);
    final ordersAsync = ref.watch(merchantOrdersProvider);

    final l10n = AppLocalizations.of(context)!;
    return dashboardAsync.when(
      data: (merchant) => _buildContent(context, theme, ref, merchant, ordersAsync, l10n),
      loading: () => const Scaffold(body: LoadingSkeleton(itemCount: 3)),
      error: (error, _) => Scaffold(
        body: ErrorWidgetView(
          title: l10n.failedToLoadDashboard,
          subtitle: error.toString(),
          onRetry: () => ref.invalidate(merchantDashboardProvider),
        ),
      ),
    );
  }

  Widget _buildContent(BuildContext context, ThemeData theme, WidgetRef ref, Merchant merchant, AsyncValue<List<Order>> ordersAsync, AppLocalizations l10n) {
    final orderCount = ordersAsync.when(data: (o) => o.length, loading: () => 0, error: (_, __) => 0);

    return Scaffold(
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            _buildHeader(context, theme, merchant, l10n),
            const SizedBox(height: 20),
            _buildKpiRow(context, theme, merchant, orderCount, l10n),
            const SizedBox(height: 24),
            _buildQuickActions(theme, context, l10n),
            const SizedBox(height: 24),
            _buildRecentOrders(theme, context, ordersAsync, l10n),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(BuildContext context, ThemeData theme, Merchant merchant, AppLocalizations l10n) {
    final isOnline = merchant.isOnline;
    return Row(
      children: [
        Container(
          width: 48, height: 48,
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
              Text(merchant.storeName, style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
              Text(
                '${isOnline ? l10n.online : l10n.offline} · ${l10n.activeOffersCount(merchant.activeOfferCount)}',
                style: AppTextStyles.caption.copyWith(color: isOnline ? AppColors.success : AppColors.grey500),
              ),
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

  Widget _buildKpiRow(BuildContext context, ThemeData theme, Merchant merchant, int orderCount, AppLocalizations l10n) {
    return Row(
      children: [
        _buildKpiCard(theme, l10n.todaysRevenue, '\$${merchant.todayRevenue.toStringAsFixed(2)}', Icons.trending_up_rounded, AppColors.success),
        const SizedBox(width: 12),
        _buildKpiCard(theme, l10n.orders, '$orderCount', Icons.shopping_bag_rounded, AppColors.secondary),
      ],
    );
  }

  Widget _buildKpiCard(ThemeData theme, String label, String value, IconData icon, Color color) {
    return Expanded(
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: theme.cardColor,
          borderRadius: BorderRadius.circular(14),
          boxShadow: [BoxShadow(color: Colors.black.withAlpha(8), blurRadius: 6, offset: const Offset(0, 2))],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(color: color.withAlpha(26), borderRadius: BorderRadius.circular(10)),
              child: Icon(icon, color: color, size: 22),
            ),
            const SizedBox(height: 12),
            Text(value, style: theme.textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 4),
            Text(label, style: AppTextStyles.caption.copyWith(color: AppColors.grey500)),
          ],
        ),
      ),
    );
  }

  Widget _buildQuickActions(ThemeData theme, BuildContext context, AppLocalizations l10n) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(l10n.quickActions, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
        const SizedBox(height: 12),
        Row(
          children: [
            _buildActionButton(context, l10n.createOffer, Icons.add_circle_outline, AppColors.secondary, () => context.push('/merchant/offers/create')),
            const SizedBox(width: 12),
            _buildActionButton(context, l10n.viewOffers, Icons.local_offer_outlined, AppColors.primary, () => context.push('/merchant/offers')),
            const SizedBox(width: 12),
            _buildActionButton(context, l10n.reports, Icons.bar_chart_rounded, AppColors.warning, () => context.push('/merchant/reports')),
          ],
        ),
      ],
    );
  }

  Widget _buildActionButton(BuildContext context, String label, IconData icon, Color color, VoidCallback onTap) {
    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: color.withAlpha(13),
            borderRadius: BorderRadius.circular(14),
          ),
          child: Column(
            children: [
              Icon(icon, color: color, size: 26),
              const SizedBox(height: 8),
              Text(label, style: AppTextStyles.caption.copyWith(fontWeight: FontWeight.w600), textAlign: TextAlign.center),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildRecentOrders(ThemeData theme, BuildContext context, AsyncValue ordersAsync, AppLocalizations l10n) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(l10n.recentOrders, style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
            TextButton(onPressed: () => context.push('/merchant/orders'), child: Text(l10n.viewAll)),
          ],
        ),
        ordersAsync.when(
          data: (orders) {
            final recent = orders.take(3).toList();
            return Column(
              children: recent.map((order) => Container(
                margin: const EdgeInsets.only(bottom: 8),
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: theme.cardColor,
                  borderRadius: BorderRadius.circular(12),
                  boxShadow: [BoxShadow(color: Colors.black.withAlpha(8), blurRadius: 6, offset: const Offset(0, 2))],
                ),
                child: Row(
                  children: [
                    Container(
                      width: 40, height: 40,
                      decoration: BoxDecoration(color: AppColors.secondary.withAlpha(26), borderRadius: BorderRadius.circular(10)),
                      child: const Icon(Icons.receipt_long_rounded, color: AppColors.secondary, size: 20),
                    ),
                    const SizedBox(width: 12),
                    Expanded(child: Text(l10n.order(order.id.substring(0, 6)), style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600))),
                    Text('\$${order.total.toStringAsFixed(2)}', style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.bold, color: AppColors.secondary)),
                  ],
                ),
              )).toList(),
            );
          },
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (_, __) => Text(l10n.couldNotLoadOrders),
        ),
      ],
    );
  }
}
