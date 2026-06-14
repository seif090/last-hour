import 'package:flutter/material.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';
import '../../../../shared/widgets/custom_button.dart';
import '../../../../shared/widgets/custom_text_field.dart';

class MerchantCreateOfferScreen extends StatefulWidget {
  const MerchantCreateOfferScreen({super.key});

  @override
  State<MerchantCreateOfferScreen> createState() => _MerchantCreateOfferScreenState();
}

class _MerchantCreateOfferScreenState extends State<MerchantCreateOfferScreen> {
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _descController = TextEditingController();
  final _origPriceController = TextEditingController();
  final _discPriceController = TextEditingController();
  final _quantityController = TextEditingController();
  String _selectedCategory = 'Food';

  @override
  void dispose() {
    _titleController.dispose();
    _descController.dispose();
    _origPriceController.dispose();
    _discPriceController.dispose();
    _quantityController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(AppLocalizations.of(context)!.createOffer),
        centerTitle: true,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _buildImagePicker(theme),
              const SizedBox(height: 20),
              CustomTextField(
                controller: _titleController,
                label: AppLocalizations.of(context)!.offerTitle,
                hintText: AppLocalizations.of(context)!.offerTitleHint,
                prefixIcon: const Icon(Icons.local_offer_outlined),
                validator: (v) => v == null || v.isEmpty ? AppLocalizations.of(context)!.required : null,
              ),
              const SizedBox(height: 16),
              CustomTextField(
                controller: _descController,
                label: AppLocalizations.of(context)!.description,
                hintText: AppLocalizations.of(context)!.descriptionHint,
                maxLines: 3,
                prefixIcon: const Icon(Icons.description_outlined),
                validator: (v) => v == null || v.isEmpty ? AppLocalizations.of(context)!.required : null,
              ),
              const SizedBox(height: 16),
              Text(AppLocalizations.of(context)!.category, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              _buildCategorySelector(),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: CustomTextField(
                      controller: _origPriceController,
                      label: AppLocalizations.of(context)!.originalPrice,
                      hintText: '0.00',
                      keyboardType: TextInputType.number,
                      prefixIcon: const Icon(Icons.attach_money),
                      validator: (v) => v == null || v.isEmpty ? AppLocalizations.of(context)!.required : null,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: CustomTextField(
                      controller: _discPriceController,
                      label: AppLocalizations.of(context)!.discountPrice,
                      hintText: '0.00',
                      keyboardType: TextInputType.number,
                      prefixIcon: const Icon(Icons.discount_outlined),
                      validator: (v) => v == null || v.isEmpty ? AppLocalizations.of(context)!.required : null,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              CustomTextField(
                controller: _quantityController,
                label: AppLocalizations.of(context)!.quantityAvailable,
                hintText: AppLocalizations.of(context)!.quantityHint,
                keyboardType: TextInputType.number,
                prefixIcon: const Icon(Icons.inventory_2_outlined),
                validator: (v) => v == null || v.isEmpty ? AppLocalizations.of(context)!.required : null,
              ),
              const SizedBox(height: 16),
              Text(AppLocalizations.of(context)!.pickupWindow, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.w600)),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () {},
                      icon: const Icon(Icons.schedule, size: 18),
                      label: Text(AppLocalizations.of(context)!.startTime),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: AppColors.grey600,
                        side: BorderSide(color: AppColors.grey300),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        padding: const EdgeInsets.symmetric(vertical: 14),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () {},
                      icon: const Icon(Icons.schedule, size: 18),
                      label: Text(AppLocalizations.of(context)!.endTime),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: AppColors.grey600,
                        side: BorderSide(color: AppColors.grey300),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                        padding: const EdgeInsets.symmetric(vertical: 14),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 32),
              CustomButton(
                label: AppLocalizations.of(context)!.createOffer,
                onPressed: () {
                  if (_formKey.currentState?.validate() ?? false) {}
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildImagePicker(ThemeData theme) {
    return GestureDetector(
      onTap: () {},
      child: Container(
        width: double.infinity,
        height: 180,
        decoration: BoxDecoration(
          color: AppColors.grey100,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: AppColors.grey200, width: 2, strokeAlign: BorderSide.strokeAlignInside),
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 48, height: 48,
              decoration: BoxDecoration(
                color: AppColors.primary.withAlpha(26),
                borderRadius: BorderRadius.circular(12),
              ),
              child: const Icon(Icons.add_photo_alternate_outlined, color: AppColors.primary, size: 24),
            ),
            const SizedBox(height: 12),
            Text(AppLocalizations.of(context)!.addPhoto, style: AppTextStyles.labelLarge.copyWith(fontWeight: FontWeight.w600)),
            Text(AppLocalizations.of(context)!.uploadImageHint, style: AppTextStyles.caption),
          ],
        ),
      ),
    );
  }

  Widget _buildCategorySelector() {
    final l10n = AppLocalizations.of(context)!;
    final categories = ['Food', 'Bakery', 'Desserts', 'Groceries', 'Beverages'];
    final categoryLabels = {
      'Food': l10n.food,
      'Bakery': l10n.bakery,
      'Desserts': l10n.desserts,
      'Groceries': l10n.groceries,
      'Beverages': l10n.beverages,
    };
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: categories.map((cat) {
          final isSelected = _selectedCategory == cat;
          return Padding(
            padding: const EdgeInsets.only(right: 8),
            child:             ChoiceChip(
              label: Text(categoryLabels[cat]!),
              selected: isSelected,
              onSelected: (v) => setState(() => _selectedCategory = cat),
              selectedColor: AppColors.primary,
              labelStyle: TextStyle(
                color: isSelected ? Colors.white : AppColors.grey700,
                fontWeight: FontWeight.w500,
                fontSize: 13,
              ),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
              side: BorderSide(color: isSelected ? AppColors.primary : AppColors.grey300),
            ),
          );
        }).toList(),
      ),
    );
  }
}
