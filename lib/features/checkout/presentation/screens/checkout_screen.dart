import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/custom_button.dart';

class CheckoutScreen extends StatefulWidget {
  const CheckoutScreen({super.key});

  @override
  State<CheckoutScreen> createState() => _CheckoutScreenState();
}

class _CheckoutScreenState extends State<CheckoutScreen> {
  int _currentStep = 0;
  String _deliveryMode = 'pickup';
  String _selectedAddress = 'Home';
  String _selectedPayment = 'Cash';

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Checkout'),
        centerTitle: true,
      ),
      body: Column(
        children: [
          _buildStepIndicator(theme),
          Expanded(
            child: IndexedStack(
              index: _currentStep,
              children: [
                _buildDeliveryStep(theme),
                _buildPaymentStep(theme),
                _buildReviewStep(theme),
              ],
            ),
          ),
          _buildBottomBar(theme),
        ],
      ),
    );
  }

  Widget _buildStepIndicator(ThemeData theme) {
    final steps = ['Delivery', 'Payment', 'Review'];
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
      color: theme.colorScheme.surface,
      child: Row(
        children: List.generate(steps.length, (i) {
          final isActive = i == _currentStep;
          final isCompleted = i < _currentStep;
          return Expanded(
            child: Row(
              children: [
                if (i > 0)
                  Expanded(
                    child: Container(
                      height: 2,
                      color: isCompleted
                          ? AppColors.primary
                          : AppColors.grey200,
                    ),
                  ),
                Container(
                  width: 32,
                  height: 32,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: isCompleted
                        ? AppColors.primary
                        : isActive
                            ? AppColors.primaryLight
                            : AppColors.grey200,
                  ),
                  child: Center(
                    child: isCompleted
                        ? const Icon(Icons.check, size: 18, color: Colors.white)
                        : Text(
                            '${i + 1}',
                            style: AppTextStyles.labelMedium.copyWith(
                              color: isActive ? Colors.white : AppColors.grey500,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                  ),
                ),
                if (i < steps.length - 1)
                  Expanded(
                    child: Container(
                      height: 2,
                      color: i <= _currentStep
                          ? AppColors.primary
                          : AppColors.grey200,
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
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text('Delivery Mode', style: AppTextStyles.titleMedium.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: _buildModeCard('Pickup', Icons.store_rounded, _deliveryMode == 'pickup'),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildModeCard('Delivery', Icons.delivery_dining_rounded, _deliveryMode == 'delivery'),
            ),
          ],
        ),
        const SizedBox(height: 24),
        Text('Delivery Address', style: AppTextStyles.titleMedium.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 12),
        _buildAddressCard(theme, 'Home', '123 Main St, Apt 4B', _selectedAddress == 'Home'),
        const SizedBox(height: 8),
        _buildAddressCard(theme, 'Work', '456 Oak Ave, Suite 200', _selectedAddress == 'Work'),
        const SizedBox(height: 8),
        OutlinedButton.icon(
          onPressed: () {},
          icon: const Icon(Icons.add_rounded),
          label: const Text('Add New Address'),
          style: OutlinedButton.styleFrom(
            foregroundColor: AppColors.primary,
            side: BorderSide(color: AppColors.primary.withAlpha(77)),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            padding: const EdgeInsets.symmetric(vertical: 14),
          ),
        ),
      ],
    );
  }

  Widget _buildModeCard(String label, IconData icon, bool isSelected) {
    final cardTheme = Theme.of(context);
    return GestureDetector(
      onTap: () => setState(() => _deliveryMode = label.toLowerCase()),
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 20, horizontal: 16),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.primary.withAlpha(20) : cardTheme.cardColor,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: isSelected ? AppColors.primary : AppColors.grey200,
            width: isSelected ? 2 : 1,
          ),
        ),
        child: Column(
          children: [
            Icon(icon, size: 32, color: isSelected ? AppColors.primary : AppColors.grey400),
            const SizedBox(height: 8),
            Text(label, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.w600)),
          ],
        ),
      ),
    );
  }

  Widget _buildAddressCard(ThemeData theme, String label, String address, bool isSelected) {
    return GestureDetector(
      onTap: () => setState(() => _selectedAddress = label),
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.primary.withAlpha(15) : null,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: isSelected ? AppColors.primary : AppColors.grey200,
          ),
        ),
        child: Row(
          children: [
            Icon(
              isSelected ? Icons.radio_button_checked : Icons.radio_button_unchecked,
              color: isSelected ? AppColors.primary : AppColors.grey400,
              size: 22,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.w600)),
                  const SizedBox(height: 2),
                  Text(address, style: AppTextStyles.caption),
                ],
              ),
            ),
            Icon(Icons.edit_outlined, size: 18, color: AppColors.grey400),
          ],
        ),
      ),
    );
  }

  Widget _buildPaymentStep(ThemeData theme) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text('Payment Method', style: AppTextStyles.titleMedium.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 12),
        _buildPaymentCard('Cash', Icons.money_rounded, 'Pay when you pick up'),
        const SizedBox(height: 8),
        _buildPaymentCard('Card', Icons.credit_card_rounded, 'Pay with credit or debit card'),
        const SizedBox(height: 8),
        _buildPaymentCard('Wallet', Icons.account_balance_wallet_rounded, 'Balance: \$45.00'),
        const SizedBox(height: 24),
        Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: AppColors.info.withAlpha(15),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: AppColors.info.withAlpha(51)),
          ),
          child: Row(
            children: [
              Icon(Icons.info_outline, color: AppColors.info, size: 20),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  'Your payment is processed securely. We never store your card details.',
                  style: AppTextStyles.caption.copyWith(color: AppColors.grey600),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildPaymentCard(String label, IconData icon, String subtitle) {
    final isSelected = _selectedPayment == label;
    return GestureDetector(
      onTap: () => setState(() => _selectedPayment = label),
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.primary.withAlpha(15) : null,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: isSelected ? AppColors.primary : AppColors.grey200),
        ),
        child: Row(
          children: [
            Icon(icon, size: 28, color: isSelected ? AppColors.primary : AppColors.grey500),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(label, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.w600)),
                  Text(subtitle, style: AppTextStyles.caption),
                ],
              ),
            ),
            Icon(
              isSelected ? Icons.radio_button_checked : Icons.radio_button_unchecked,
              color: isSelected ? AppColors.primary : AppColors.grey400,
              size: 22,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildReviewStep(ThemeData theme) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _buildReviewSection(theme, 'Delivery', 'Pickup - Sakura Japanese\n123 Main St (5 min away)'),
        const SizedBox(height: 16),
        _buildReviewSection(theme, 'Payment', 'Cash on pickup'),
        const SizedBox(height: 16),
        Text('Order Items', style: AppTextStyles.titleMedium.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 8),
        _buildReviewItem('Mixed Sushi Box', '\$9.99', '2x'),
        const Divider(height: 1),
        _buildReviewItem('Artisan Croissants', '\$6.50', '1x'),
        const Divider(height: 1),
        _buildReviewItem('Chocolate Cake Slice', '\$3.99', '3x'),
        const Padding(
          padding: EdgeInsets.symmetric(vertical: 8),
          child: Divider(),
        ),
        _buildSummaryRow('Subtotal', '\$29.95'),
        _buildSummaryRow('Service Fee', '\$1.99'),
        _buildSummaryRow('Total', '\$31.94', isTotal: true),
      ],
    );
  }

  Widget _buildReviewSection(ThemeData theme, String title, String content) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(Icons.check_circle, color: AppColors.primary, size: 20),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: AppTextStyles.labelMedium.copyWith(color: AppColors.grey500)),
              const SizedBox(height: 2),
              Text(content, style: AppTextStyles.bodyMedium),
            ],
          ),
        ),
        TextButton(
          onPressed: () => setState(() => _currentStep = title == 'Delivery' ? 0 : 1),
          child: const Text('Edit', style: TextStyle(color: AppColors.primary)),
        ),
      ],
    );
  }

  Widget _buildReviewItem(String name, String price, String qty) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        children: [
          Expanded(
            child: Text(name, style: AppTextStyles.bodyMedium),
          ),
          Text('$qty  ', style: AppTextStyles.bodyMedium.copyWith(color: AppColors.grey500)),
          Text(price, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }

  Widget _buildSummaryRow(String label, String value, {bool isTotal = false}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(
            label,
            style: (isTotal ? AppTextStyles.titleSmall : AppTextStyles.bodyMedium)
                .copyWith(fontWeight: isTotal ? FontWeight.bold : FontWeight.normal),
          ),
          Text(
            value,
            style: (isTotal ? AppTextStyles.titleSmall : AppTextStyles.bodyMedium)
                .copyWith(fontWeight: FontWeight.bold),
          ),
        ],
      ),
    );
  }

  Widget _buildBottomBar(ThemeData theme) {
    final isLastStep = _currentStep == 2;
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha(13),
            blurRadius: 10,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: Row(
        children: [
          if (_currentStep > 0)
            Expanded(
              child: CustomButton(
                label: 'Back',
                onPressed: () => setState(() => _currentStep--),
                type: CustomButtonType.outline,
                isFullWidth: true,
              ),
            ),
          if (_currentStep > 0) const SizedBox(width: 12),
          Expanded(
            flex: _currentStep == 0 ? 1 : 2,
            child: CustomButton(
              label: isLastStep ? 'Place Order - \$31.94' : 'Continue',
              onPressed: () {
                if (isLastStep) {
                  context.go('/order-confirmation');
                } else {
                  setState(() => _currentStep++);
                }
              },
            ),
          ),
        ],
      ),
    );
  }
}
