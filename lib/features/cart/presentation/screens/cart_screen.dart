import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import 'package:go_router/go_router.dart';
import '../../../../core/routes/route_names.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/custom_button.dart';
import '../../../../shared/widgets/error_widget_view.dart';
import '../../../../shared/widgets/empty_state_widget.dart';
import '../../../../shared/widgets/quantity_selector.dart';
import '../../domain/entities/cart_item.dart';
import '../providers/cart_providers.dart';

class CartScreen extends ConsumerStatefulWidget {
  const CartScreen({super.key});

  @override
  ConsumerState<CartScreen> createState() => _CartScreenState();
}

class _CartScreenState extends ConsumerState<CartScreen> {
  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cartAsync = ref.watch(cartItemsProvider);
    final total = ref.watch(cartTotalProvider);
    final l10n = AppLocalizations.of(context)!;

    return cartAsync.when(
      data: (items) {
        if (items.isEmpty) {
          return Scaffold(
            body: EmptyStateWidget.cart(title: l10n.cartEmptyTitle, subtitle: l10n.cartEmptySubtitle),
          );
        }
        return _buildCart(context, theme, items, total);
      },
      loading: () => const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      ),
      error: (error, _) => Scaffold(
        body: ErrorWidgetView(
          title: AppLocalizations.of(context)!.couldNotLoadCart,
          subtitle: error.toString(),
          onRetry: () => ref.invalidate(cartItemsProvider),
        ),
      ),
    );
  }

  Widget _buildCart(BuildContext context, ThemeData theme, List<CartItem> items, double total) {
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            _buildHeader(theme),
            Expanded(
              child: ListView.separated(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                itemCount: items.length,
                separatorBuilder: (_, __) => const Divider(height: 1),
                itemBuilder: (_, index) => _buildCartItem(theme, items[index]),
              ),
            ),
            _buildSummary(theme, items, total),
            _buildCheckoutButton(theme),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(ThemeData theme) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
      child: Row(
        children: [
          Text(
            AppLocalizations.of(context)!.myCart,
            style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
          ),
          const Spacer(),
          TextButton.icon(
            onPressed: () {
              ref.read(cartRepositoryProvider).clearCart();
              ref.invalidate(cartItemsProvider);
            },
            icon: const Icon(Icons.delete_outline_rounded, size: 18),
            label: Text(AppLocalizations.of(context)!.clear),
            style: TextButton.styleFrom(foregroundColor: AppColors.discountRed),
          ),
        ],
      ),
    );
  }

  Widget _buildCartItem(ThemeData theme, CartItem item) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12),
      child: Row(
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(12),
            child: Container(
              width: 72,
              height: 72,
              color: AppColors.grey200,
              child: item.imageUrl != null
                  ? Image.network(item.imageUrl!, fit: BoxFit.cover, errorBuilder: (_, __, ___) => Icon(Icons.image_rounded, color: AppColors.grey400))
                  : Icon(Icons.image_rounded, color: AppColors.grey400),
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.title,
                  style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 4),
                Text(
                  item.storeName,
                  style: AppTextStyles.caption.copyWith(color: AppColors.grey500),
                ),
                const SizedBox(height: 6),
                Text(
                  '\$${(item.price * item.quantity).toStringAsFixed(2)}',
                  style: theme.textTheme.titleSmall?.copyWith(
                    color: AppColors.primary,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
          ),
          QuantitySelector(
            quantity: item.quantity,
            maxQuantity: item.maxQuantity,
            onChanged: (val) {
              if (val == 0) {
                ref.read(cartRepositoryProvider).removeFromCart(item.id);
              } else {
                ref.read(cartRepositoryProvider).updateQuantity(
                  cartItemId: item.id,
                  quantity: val,
                );
              }
              ref.invalidate(cartItemsProvider);
            },
          ),
        ],
      ),
    );
  }

  Widget _buildSummary(ThemeData theme, List<CartItem> items, double total) {
    final subtotal = items.fold(0.0, (sum, item) => sum + item.price * item.quantity);
    final serviceFee = 1.99;
    final grandTotal = subtotal + serviceFee;

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        border: Border(top: BorderSide(color: AppColors.grey200)),
      ),
      child: Column(
        children: [
          _buildSummaryRow(AppLocalizations.of(context)!.subtotal, '\$${subtotal.toStringAsFixed(2)}'),
          const SizedBox(height: 8),
          _buildSummaryRow(AppLocalizations.of(context)!.serviceFee, '\$${serviceFee.toStringAsFixed(2)}'),
          const Divider(height: 20),
          _buildSummaryRow(AppLocalizations.of(context)!.total, '\$${grandTotal.toStringAsFixed(2)}', isTotal: true),
        ],
      ),
    );
  }

  Widget _buildSummaryRow(String label, String value, {bool isTotal = false}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: AppTextStyles.bodyMedium.copyWith(
            fontWeight: isTotal ? FontWeight.bold : FontWeight.normal,
            color: isTotal ? AppColors.grey900 : AppColors.grey600,
          ),
        ),
        Text(
          value,
          style: AppTextStyles.bodyMedium.copyWith(
            fontWeight: isTotal ? FontWeight.bold : FontWeight.w600,
            color: isTotal ? AppColors.primary : AppColors.grey900,
          ),
        ),
      ],
    );
  }

  Widget _buildCheckoutButton(ThemeData theme) {
    return Container(
      padding: EdgeInsets.fromLTRB(16, 8, 16, MediaQuery.of(context).padding.bottom + 12),
      child: CustomButton(
        label: AppLocalizations.of(context)!.proceedToCheckout,
        onPressed: () => context.push(RouteNames.checkout),
      ),
    );
  }
}
