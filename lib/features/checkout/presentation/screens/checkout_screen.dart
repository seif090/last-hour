import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../../core/routes/route_names.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/custom_button.dart';
import '../../../cart/presentation/providers/cart_providers.dart';

class CheckoutScreen extends ConsumerStatefulWidget {
  const CheckoutScreen({super.key});

  @override
  ConsumerState<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends ConsumerState<CheckoutScreen> {
  int _currentStep = 0;
  String _deliveryMode = 'pickup';
  String _selectedAddress = 'Home';
  String _selectedPayment = 'Cash';

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cartAsync = ref.watch(cartItemsProvider);
    final total = ref.watch(cartTotalProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Checkout'),
        centerTitle: true,
      ),
      body: Column(
        children: [
          _buildStepIndicator(theme),
          Expanded(
            child: cartAsync.when(
              data: (items) => IndexedStack(
                index: _currentStep,
                children: [
                  _buildDeliveryStep(theme),
                  _buildPaymentStep(theme),
                  _buildReviewStep(theme, items, total),
                ],
              ),
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (_, __) => const Center(child: Text('Could not load cart')),
            ),
          ),
        ],
      ),
      bottomNavigationBar: _buildBottomNav(theme, total),
    );
  }

  Widget _buildStepIndicator(ThemeData theme) {
    final steps = ['Delivery', 'Payment', 'Review'];
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Row(
        children: List.generate(steps.length, (index) {
          final isActive = index <= _currentStep;
          final isLast = index == steps.length - 1;
          return Expanded(
            child: Row(
              children: [
                Container(
                  width: 32,
                  height: 32,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: isActive ? AppColors.primary : AppColors.grey300,
                  ),
                  child: Center(
                    child: Text(
                      '${index + 1}',
                      style: TextStyle(
                        color: isActive ? Colors.white : AppColors.grey600,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 6),
                Text(
                  steps[index],
                  style: AppTextStyles.caption.copyWith(
                    color: isActive ? AppColors.primary : AppColors.grey500,
                    fontWeight: isActive ? FontWeight.w600 : FontWeight.normal,
                  ),
                ),
                if (!isLast)
                  Expanded(
                    child: Container(
                      height: 2,
                      margin: const EdgeInsets.symmetric(horizontal: 8),
                      color: index < _currentStep ? AppColors.primary : AppColors.grey300,
                    ),
                  ),
              ],
            ),
          );
        }),
      ),
    );
  }

  Widget _buildDeliveryStep(ThemeData theme) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Delivery Mode', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(child: _buildModeCard('Pickup', Icons.store_rounded, _deliveryMode == 'pickup')),
              const SizedBox(width: 12),
              Expanded(child: _buildModeCard('Delivery', Icons.delivery_dining_rounded, _deliveryMode == 'delivery')),
            ],
          ),
          const SizedBox(height: 24),
          Text('Delivery Address', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: 12),
          _buildAddressCard(theme),
        ],
      ),
    );
  }

  Widget _buildModeCard(String label, IconData icon, bool isSelected) {
    return GestureDetector(
      onTap: () => setState(() => _deliveryMode = label.toLowerCase()),
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.primary.withAlpha(13) : Colors.transparent,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: isSelected ? AppColors.primary : AppColors.grey300,
            width: isSelected ? 2 : 1,
          ),
        ),
        child: Column(
          children: [
            Icon(icon, color: isSelected ? AppColors.primary : AppColors.grey500, size: 28),
            const SizedBox(height: 8),
            Text(label, style: AppTextStyles.labelMedium.copyWith(fontWeight: FontWeight.w600)),
          ],
        ),
      ),
    );
  }

  Widget _buildAddressCard(ThemeData theme) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.grey200),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: AppColors.primary.withAlpha(26),
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Icon(Icons.home_rounded, color: AppColors.primary, size: 22),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Home', style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                const SizedBox(height: 2),
                Text('123 Main Street, Downtown', style: AppTextStyles.caption.copyWith(color: AppColors.grey500)),
              ],
            ),
          ),
          const Icon(Icons.chevron_right_rounded, color: AppColors.grey400),
        ],
      ),
    );
  }

  Widget _buildPaymentStep(ThemeData theme) {
    final methods = [
      {'name': 'Cash on Pickup', 'icon': Icons.money_rounded},
      {'name': 'Credit Card', 'icon': Icons.credit_card_rounded},
      {'name': 'PayPal', 'icon': Icons.paypal_rounded},
    ];

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text('Payment Method', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
        const SizedBox(height: 12),
        ...methods.map((method) => Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: _buildPaymentCard(method['name'] as String, method['icon'] as IconData),
        )),
      ],
    );
  }

  Widget _buildPaymentCard(String name, IconData icon) {
    final isSelected = _selectedPayment == name;
    return GestureDetector(
      onTap: () => setState(() => _selectedPayment = name),
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.primary.withAlpha(13) : Colors.transparent,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: isSelected ? AppColors.primary : AppColors.grey200,
            width: isSelected ? 2 : 1,
          ),
        ),
        child: Row(
          children: [
            Icon(icon, color: isSelected ? AppColors.primary : AppColors.grey500, size: 24),
            const SizedBox(width: 12),
            Expanded(child: Text(name, style: AppTextStyles.bodyMedium)),
            if (isSelected)
              const Icon(Icons.check_circle_rounded, color: AppColors.primary, size: 22),
          ],
        ),
      ),
    );
  }

  Widget _buildReviewStep(ThemeData theme, List items, double total) {
    final serviceFee = 1.99;
    final grandTotal = total + serviceFee;

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Order Summary', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: 16),
          ...items.map((item) => Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: Row(
              children: [
                Container(
                  width: 48, height: 48,
                  decoration: BoxDecoration(
                    color: AppColors.grey200,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Icon(Icons.shopping_bag_rounded, color: AppColors.grey500),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(item.title, style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                      Text('Qty: ${item.quantity}', style: AppTextStyles.caption.copyWith(color: AppColors.grey500)),
                    ],
                  ),
                ),
                Text('\$${(item.price * item.quantity).toStringAsFixed(2)}', style: AppTextStyles.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
              ],
            ),
          )),
          const Divider(height: 24),
          _buildSummaryRow('Subtotal', '\$${total.toStringAsFixed(2)}'),
          const SizedBox(height: 8),
          _buildSummaryRow('Service Fee', '\$${serviceFee.toStringAsFixed(2)}'),
          _buildSummaryRow('Delivery', 'Free', highlight: true),
          const Divider(height: 16),
          _buildSummaryRow('Total', '\$${grandTotal.toStringAsFixed(2)}', isTotal: true),
        ],
      ),
    );
  }

  Widget _buildSummaryRow(String label, String value, {bool isTotal = false, bool highlight = false}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: AppTextStyles.bodyMedium.copyWith(
              fontWeight: isTotal ? FontWeight.bold : FontWeight.normal,
              color: highlight ? AppColors.primary : AppColors.grey600,
            ),
          ),
          Text(
            value,
            style: AppTextStyles.bodyMedium.copyWith(
              fontWeight: isTotal ? FontWeight.bold : FontWeight.w600,
              color: highlight ? AppColors.primary : AppColors.grey900,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBottomNav(ThemeData theme, double total) {
    return Container(
      padding: EdgeInsets.fromLTRB(16, 12, 16, MediaQuery.of(context).padding.bottom + 12),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        boxShadow: [
          BoxShadow(color: Colors.black.withAlpha(13), blurRadius: 10, offset: const Offset(0, -2)),
        ],
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Total', style: AppTextStyles.caption.copyWith(color: AppColors.grey500)),
                Text('\$${(total + 1.99).toStringAsFixed(2)}', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold, color: AppColors.primary)),
              ],
            ),
          ),
          if (_currentStep < 2)
            CustomButton(
              label: 'Continue',
              onPressed: () => setState(() => _currentStep++),
            )
          else
            CustomButton(
              label: 'Place Order',
              onPressed: () {
                context.go(RouteNames.orderConfirmation);
              },
            ),
        ],
      ),
    );
  }
}
