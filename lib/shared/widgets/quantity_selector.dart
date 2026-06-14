import 'package:flutter/material.dart';
import '../../core/theme/app_text_styles.dart';

class QuantitySelector extends StatelessWidget {
  final int quantity;
  final int minQuantity;
  final int maxQuantity;
  final ValueChanged<int> onChanged;

  const QuantitySelector({
    super.key,
    required this.quantity,
    this.minQuantity = 1,
    this.maxQuantity = 99,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHighest.withAlpha(80),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          _buildButton(
            icon: Icons.remove_rounded,
            onTap: quantity > minQuantity
                ? () => onChanged(quantity - 1)
                : null,
            theme: theme,
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Text(
              quantity.toString(),
              style: AppTextStyles.titleMedium.copyWith(
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
          _buildButton(
            icon: Icons.add_rounded,
            onTap: quantity < maxQuantity
                ? () => onChanged(quantity + 1)
                : null,
            theme: theme,
          ),
        ],
      ),
    );
  }

  Widget _buildButton({
    required IconData icon,
    required VoidCallback? onTap,
    required ThemeData theme,
  }) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Container(
          padding: const EdgeInsets.all(10),
          child: Icon(
            icon,
            size: 18,
            color: onTap != null
                ? theme.colorScheme.primary
                : theme.colorScheme.primary.withAlpha(51),
          ),
        ),
      ),
    );
  }
}
