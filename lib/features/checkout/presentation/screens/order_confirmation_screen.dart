import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/custom_button.dart';

class OrderConfirmationScreen extends StatelessWidget {
  const OrderConfirmationScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24),
          child: Column(
            children: [
              const Spacer(flex: 2),
              Container(
                width: 100,
                height: 100,
                decoration: BoxDecoration(
                  color: AppColors.success.withAlpha(26),
                  shape: BoxShape.circle,
                ),
                child: const Icon(
                  Icons.check_circle_rounded,
                  size: 56,
                  color: AppColors.success,
                ),
              ),
              const SizedBox(height: 24),
              Text(
                AppLocalizations.of(context)!.orderPlaced,
                style: theme.textTheme.headlineMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                AppLocalizations.of(context)!.orderPlacedSubtitle,
                style: AppTextStyles.bodyMedium.copyWith(color: AppColors.grey500),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 32),
              _buildDetailCard(theme, context),
              const Spacer(flex: 2),
              CustomButton(
                label: AppLocalizations.of(context)!.trackOrder,
                onPressed: () {},
              ),
              const SizedBox(height: 12),
              CustomButton(
                label: AppLocalizations.of(context)!.backToHome,
                onPressed: () => context.go('/main/home'),
                type: CustomButtonType.outline,
              ),
              const SizedBox(height: 16),
              Text(
                AppLocalizations.of(context)!.orderNumber('1024'),
                style: AppTextStyles.caption.copyWith(color: AppColors.grey400),
              ),
              const Spacer(),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildDetailCard(ThemeData theme, BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHighest.withAlpha(60),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        children: [
          _buildInfoRow(Icons.store_rounded, 'Sakura Japanese', AppLocalizations.of(context)!.store),
          const SizedBox(height: 14),
          _buildInfoRow(Icons.shopping_bag_rounded, 'Ready in 15-20 min', AppLocalizations.of(context)!.pickupTime),
          const SizedBox(height: 14),
          _buildInfoRow(Icons.payment_outlined, AppLocalizations.of(context)!.cashOnPickup, AppLocalizations.of(context)!.payment),
          const SizedBox(height: 14),
          _buildInfoRow(Icons.receipt_long_rounded, '\$31.94', AppLocalizations.of(context)!.orderTotal),
        ],
      ),
    );
  }

  Widget _buildInfoRow(IconData icon, String value, String label) {
    return Row(
      children: [
        Container(
          width: 40,
          height: 40,
          decoration: BoxDecoration(
            color: AppColors.primary.withAlpha(26),
            borderRadius: BorderRadius.circular(10),
          ),
          child: Icon(icon, size: 20, color: AppColors.primary),
        ),
        const SizedBox(width: 14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(value, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.w600)),
              Text(label, style: AppTextStyles.caption.copyWith(fontSize: 11)),
            ],
          ),
        ),
      ],
    );
  }
}
