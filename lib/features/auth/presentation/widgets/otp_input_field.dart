import 'package:flutter/material.dart';
import '../../../../core/theme/app_colors.dart';

class OtpInputField extends StatelessWidget {
  final TextEditingController controller;
  final ValueChanged<String>? onChanged;

  const OtpInputField({
    super.key,
    required this.controller,
    this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return FormField<String>(
      builder: (field) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: List.generate(6, (index) {
                return _OtpDigitBox(
                  index: index,
                  controller: controller,
                  onChanged: onChanged,
                );
              }),
            ),
            if (field.errorText != null)
              Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Text(
                  field.errorText!,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Theme.of(context).colorScheme.error,
                  ),
                ),
              ),
          ],
        );
      },
    );
  }
}

class _OtpDigitBox extends StatelessWidget {
  final int index;
  final TextEditingController controller;
  final ValueChanged<String>? onChanged;

  const _OtpDigitBox({
    required this.index,
    required this.controller,
    this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final digit = controller.text.length > index
        ? controller.text[index]
        : null;

    return GestureDetector(
      onTap: () => _showDigitPicker(context),
      child: Container(
        width: 48,
        height: 56,
        decoration: BoxDecoration(
          color: digit != null
              ? AppColors.primary.withAlpha(26)
              : theme.colorScheme.surfaceContainerHighest.withAlpha(80),
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: digit != null
                ? AppColors.primary
                : Colors.transparent,
            width: 1.5,
          ),
        ),
        child: Center(
          child: Text(
            digit ?? '_',
            style: theme.textTheme.headlineSmall?.copyWith(
              fontWeight: FontWeight.bold,
              color: digit != null
                  ? AppColors.primary
                  : AppColors.grey400,
            ),
          ),
        ),
      ),
    );
  }

  void _showDigitPicker(BuildContext context) {
    showModalBottomSheet(
      context: context,
      builder: (ctx) => _OtpPicker(
        onDigitSelected: (digit) {
          final text = controller.text;
          if (index < text.length) {
            final chars = text.split('');
            chars[index] = digit;
            controller.text = chars.join();
          } else {
            controller.text = '$text$digit';
          }
          controller.selection = TextSelection.collapsed(
            offset: controller.text.length,
          );
          onChanged?.call(controller.text);
          Navigator.of(ctx).pop();
        },
        onDelete: () {
          if (index < controller.text.length) {
            final chars = controller.text.split('');
            chars.removeAt(index);
            controller.text = chars.join();
          } else if (controller.text.isNotEmpty) {
            controller.text = controller.text.substring(
              0,
              controller.text.length - 1,
            );
          }
          onChanged?.call(controller.text);
          Navigator.of(ctx).pop();
        },
      ),
    );
  }
}

class _OtpPicker extends StatelessWidget {
  final ValueChanged<String> onDigitSelected;
  final VoidCallback onDelete;

  const _OtpPicker({
    required this.onDigitSelected,
    required this.onDelete,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 40,
            height: 4,
            decoration: BoxDecoration(
              color: AppColors.grey300,
              borderRadius: BorderRadius.circular(2),
            ),
          ),
          const SizedBox(height: 24),
          GridView.count(
            crossAxisCount: 3,
            shrinkWrap: true,
            mainAxisSpacing: 12,
            crossAxisSpacing: 12,
            physics: const NeverScrollableScrollPhysics(),
            children: [
              for (int i = 1; i <= 9; i++)
                _DigitButton(
                  digit: '$i',
                  onTap: () => onDigitSelected('$i'),
                ),
              _DigitButton(
                icon: Icons.backspace_outlined,
                onTap: onDelete,
              ),
              _DigitButton(
                digit: '0',
                onTap: () => onDigitSelected('0'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _DigitButton extends StatelessWidget {
  final String? digit;
  final IconData? icon;
  final VoidCallback onTap;

  const _DigitButton({
    this.digit,
    this.icon,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Container(
          height: 56,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(12),
            color: Theme.of(context).colorScheme.surfaceContainerHighest.withAlpha(80),
          ),
          child: Center(
            child: digit != null
                ? Text(
                    digit!,
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  )
                : Icon(icon, size: 24),
          ),
        ),
      ),
    );
  }
}
