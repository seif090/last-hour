import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import 'package:go_router/go_router.dart';
import '../../../../core/routes/route_names.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../core/utils/helpers.dart';
import '../../../../shared/widgets/error_widget_view.dart';
import '../../../../shared/widgets/empty_state_widget.dart';
import '../../domain/entities/order.dart';
import '../providers/order_providers.dart';

class OrdersScreen extends ConsumerStatefulWidget {
  const OrdersScreen({super.key});

  @override
  ConsumerState<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends ConsumerState<OrdersScreen> {
  String _selectedTab = 'Active';

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    final status = _statusFromTab(_selectedTab);
    final ordersAsync = ref.watch(ordersByStatusProvider(status));

    return Scaffold(
      body: SafeArea(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
              child: Text(
                AppLocalizations.of(context)!.myOrders,
                style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
              ),
            ),
            _buildTabBar(theme),
            Expanded(
              child: ordersAsync.when(
                data: (orders) {
                  if (orders.isEmpty) {
                    return EmptyStateWidget.orders(title: AppLocalizations.of(context)!.noOrdersTitle, subtitle: AppLocalizations.of(context)!.noOrdersSubtitle);
                  }
                  return ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: orders.length,
                    itemBuilder: (_, index) => _buildOrderCard(theme, orders[index]),
                  );
                },
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (error, _) => ErrorWidgetView(
                  title: AppLocalizations.of(context)!.couldNotLoadOrders,
                  subtitle: error.toString(),
                  onRetry: () => ref.invalidate(ordersByStatusProvider(status)),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  OrderStatus? _statusFromTab(String tab) {
    switch (tab) {
      case 'Active': return null;
      case 'Completed': return OrderStatus.delivered;
      case 'Cancelled': return OrderStatus.cancelled;
      default: return null;
    }
  }

  String _tabLabel(String tab) {
    switch (tab) {
      case 'Active': return AppLocalizations.of(context)!.active;
      case 'Completed': return AppLocalizations.of(context)!.completed;
      case 'Cancelled': return AppLocalizations.of(context)!.cancelled;
      default: return tab;
    }
  }

  Widget _buildTabBar(ThemeData theme) {
    final tabs = ['Active', 'Completed', 'Cancelled'];
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16),
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: tabs.map((tab) {
            final isSelected = _selectedTab == tab;
            return Padding(
              padding: const EdgeInsets.only(right: 8),
              child: GestureDetector(
                onTap: () => setState(() => _selectedTab = tab),
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
                  decoration: BoxDecoration(
                    color: isSelected ? AppColors.primary : Colors.transparent,
                    borderRadius: BorderRadius.circular(22),
                    border: Border.all(
                      color: isSelected ? AppColors.primary : AppColors.grey300,
                    ),
                  ),
                  child: Text(
                    _tabLabel(tab),
                    style: AppTextStyles.labelMedium.copyWith(
                      color: isSelected ? Colors.white : AppColors.grey700,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ),
            );
          }).toList(),
        ),
      ),
    );
  }

  Widget _buildOrderCard(ThemeData theme, Order order) {
    final isActive = order.status == OrderStatus.pending || order.status == OrderStatus.confirmed || order.status == OrderStatus.preparing || order.status == OrderStatus.ready;

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.grey200),
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: () => context.push('${RouteNames.orderDetails}/${order.id}'),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      color: AppColors.primary.withAlpha(26),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Icon(Icons.store_rounded, color: AppColors.primary, size: 24),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          order.storeName,
                          style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          Helpers.formatDate(order.createdAt),
                          style: AppTextStyles.caption.copyWith(color: AppColors.grey500),
                        ),
                      ],
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: isActive ? AppColors.primary.withAlpha(26) : AppColors.grey200,
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Text(
                      order.status.name.toUpperCase(),
                      style: AppTextStyles.caption.copyWith(
                        color: isActive ? AppColors.primary : AppColors.grey600,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
              ),
              const Divider(height: 20),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    AppLocalizations.of(context)!.itemCount(order.items.length),
                    style: AppTextStyles.bodyMedium.copyWith(color: AppColors.grey600),
                  ),
                  Text(
                    '\$${order.total.toStringAsFixed(2)}',
                    style: theme.textTheme.titleSmall?.copyWith(
                      color: AppColors.primary,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
