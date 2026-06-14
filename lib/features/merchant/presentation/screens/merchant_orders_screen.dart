import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/empty_state_widget.dart';
import '../../../orders/domain/entities/order.dart';
import '../providers/merchant_providers.dart';

class MerchantOrdersScreen extends ConsumerStatefulWidget {
  const MerchantOrdersScreen({super.key});

  @override
  ConsumerState<MerchantOrdersScreen> createState() => _MerchantOrdersScreenState();
}

class _MerchantOrdersScreenState extends ConsumerState<MerchantOrdersScreen> {
  String _selectedTab = 'New';

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final ordersAsync = ref.watch(merchantOrdersProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Orders'),
        centerTitle: true,
      ),
      body: Column(
        children: [
          _buildTabBar(),
          Expanded(
            child: ordersAsync.when(
              data: (orders) {
                final filtered = _filterOrders(orders);
                if (filtered.isEmpty) return const EmptyStateWidget.orders();
                return ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: filtered.length,
                  itemBuilder: (_, i) => _buildOrderCard(theme, filtered[i]),
                );
              },
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (_, __) => Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Text('Failed to load orders'),
                    const SizedBox(height: 8),
                    TextButton(onPressed: () => ref.invalidate(merchantOrdersProvider), child: const Text('Retry')),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  List<Order> _filterOrders(List<Order> orders) {
    switch (_selectedTab) {
      case 'New': return orders.where((o) => o.status == OrderStatus.pending).toList();
      case 'Preparing': return orders.where((o) => o.status == OrderStatus.preparing).toList();
      case 'Ready': return orders.where((o) => o.status == OrderStatus.ready).toList();
      case 'Completed': return orders.where((o) => o.status == OrderStatus.delivered || o.status == OrderStatus.pickedUp).toList();
      default: return orders;
    }
  }

  Widget _buildTabBar() {
    final tabs = ['New', 'Preparing', 'Ready', 'Completed'];
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: tabs.map((t) {
            final isActive = _selectedTab == t;
            return GestureDetector(
              onTap: () => setState(() => _selectedTab = t),
              child: Container(
                margin: const EdgeInsets.only(right: 8),
                padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 8),
                decoration: BoxDecoration(
                  color: isActive ? AppColors.secondary : Colors.transparent,
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(color: isActive ? AppColors.secondary : AppColors.grey300),
                ),
                child: Text(
                  t,
                  style: AppTextStyles.labelMedium.copyWith(
                    color: isActive ? Colors.white : AppColors.grey700,
                    fontWeight: FontWeight.w600,
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
    final statusColors = {
      OrderStatus.pending: AppColors.warning,
      OrderStatus.preparing: AppColors.secondary,
      OrderStatus.ready: AppColors.success,
      OrderStatus.delivered: AppColors.primary,
      OrderStatus.pickedUp: AppColors.primary,
      OrderStatus.cancelled: AppColors.discountRed,
    };

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: theme.cardColor,
        borderRadius: BorderRadius.circular(14),
        boxShadow: [BoxShadow(color: Colors.black.withAlpha(8), blurRadius: 6, offset: const Offset(0, 2))],
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(14),
        onTap: () => context.push('/merchant/order/${order.id}'),
        child: Row(
          children: [
            Container(
              width: 48, height: 48,
              decoration: BoxDecoration(
                color: (statusColors[order.status] ?? AppColors.grey400).withAlpha(26),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(Icons.receipt_long_rounded, color: statusColors[order.status] ?? AppColors.grey400, size: 24),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Text('Order #${order.id.length > 6 ? order.id.substring(0, 6) : order.id}',
                        style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                      const Spacer(),
                      Text('\$${order.total.toStringAsFixed(2)}',
                        style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.bold, color: AppColors.secondary)),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text('${order.items.length} items',
                    style: AppTextStyles.caption.copyWith(color: AppColors.grey500)),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
