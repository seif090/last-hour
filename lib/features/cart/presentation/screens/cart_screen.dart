import 'package:flutter/material.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/custom_button.dart';
import '../../../../shared/widgets/empty_state_widget.dart';
import '../../../../shared/widgets/quantity_selector.dart';

class CartScreen extends StatefulWidget {
  const CartScreen({super.key});

  @override
  State<CartScreen> createState() => _CartScreenState();
}

class _CartScreenState extends State<CartScreen> {
  bool _isEmpty = false;

  final _items = [
    _CartItem('Mixed Sushi Box', 'Sakura Japanese', 9.99, 2, 3),
    _CartItem('Artisan Croissants', 'Le Pain Bakery', 6.50, 1, 5),
    _CartItem('Chocolate Cake Slice', 'Sweet Dreams', 3.99, 3, 2),
  ];

  double get _subtotal => _items.fold(0, (sum, item) => sum + item.price * item.quantity);
  double get _serviceFee => 1.99;
  double get _total => _subtotal + _serviceFee;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (_isEmpty) {
      return Scaffold(
        body: const EmptyStateWidget.cart(),
      );
    }

    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            _buildHeader(theme),
            Expanded(
              child: ListView.separated(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                itemCount: _items.length,
                separatorBuilder: (_, __) => const Divider(height: 1),
                itemBuilder: (_, index) => _buildCartItem(theme, _items[index]),
              ),
            ),
            _buildCouponSection(theme),
            _buildSummary(theme),
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
            'Your Cart',
            style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
          ),
          const Spacer(),
          Text(
            '${_items.length} items',
            style: AppTextStyles.bodyMedium.copyWith(color: AppColors.grey500),
          ),
        ],
      ),
    );
  }

  Widget _buildCartItem(ThemeData theme, _CartItem item) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12),
      child: Row(
        children: [
          Container(
            width: 72,
            height: 72,
            decoration: BoxDecoration(
              color: AppColors.grey200,
              borderRadius: BorderRadius.circular(14),
            ),
            child: const Icon(Icons.image_outlined, color: AppColors.grey400),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.name,
                  style: AppTextStyles.titleSmall.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  item.store,
                  style: AppTextStyles.caption.copyWith(fontSize: 11),
                ),
                const SizedBox(height: 8),
                Text(
                  '\$${(item.price * item.quantity).toStringAsFixed(2)}',
                  style: AppTextStyles.price.copyWith(color: AppColors.primary),
                ),
              ],
            ),
          ),
          QuantitySelector(
            quantity: item.quantity,
            minQuantity: 0,
            maxQuantity: item.maxQuantity,
            onChanged: (q) {
              setState(() {
                if (q == 0) {
                  _items.remove(item);
                } else {
                  item.quantity = q;
                }
              });
            },
          ),
        ],
      ),
    );
  }

  Widget _buildCouponSection(ThemeData theme) {
    return Container(
      margin: const EdgeInsets.fromLTRB(16, 8, 16, 0),
      decoration: BoxDecoration(
        border: Border.all(color: AppColors.grey200),
        borderRadius: BorderRadius.circular(14),
      ),
      child: ListTile(
        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 2),
        leading: const Icon(Icons.local_offer_outlined, color: AppColors.primary),
        title: Text(
          'Apply Coupon',
          style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.w600),
        ),
        trailing: const Icon(Icons.chevron_right_rounded),
        onTap: () {},
      ),
    );
  }

  Widget _buildSummary(ThemeData theme) {
    return Container(
      margin: const EdgeInsets.all(16),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHighest.withAlpha(60),
        borderRadius: BorderRadius.circular(14),
      ),
      child: Column(
        children: [
          _buildSummaryRow('Subtotal', '\$${_subtotal.toStringAsFixed(2)}'),
          const SizedBox(height: 8),
          _buildSummaryRow('Service Fee', '\$${_serviceFee.toStringAsFixed(2)}'),
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 8),
            child: Divider(),
          ),
          _buildSummaryRow(
            'Total',
            '\$${_total.toStringAsFixed(2)}',
            isTotal: true,
          ),
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
          style: (isTotal ? AppTextStyles.titleSmall : AppTextStyles.bodyMedium)
              .copyWith(
            fontWeight: isTotal ? FontWeight.bold : FontWeight.normal,
            color: isTotal ? null : AppColors.grey600,
          ),
        ),
        Text(
          value,
          style: (isTotal ? AppTextStyles.titleSmall : AppTextStyles.bodyMedium)
              .copyWith(fontWeight: FontWeight.bold),
        ),
      ],
    );
  }

  Widget _buildCheckoutButton(ThemeData theme) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
      child: CustomButton(
        label: 'Checkout - \$${_total.toStringAsFixed(2)}',
        onPressed: () {},
      ),
    );
  }
}

class _CartItem {
  final String name;
  final String store;
  final double price;
  int quantity;
  final int maxQuantity;

  _CartItem(this.name, this.store, this.price, this.quantity, this.maxQuantity);
}
