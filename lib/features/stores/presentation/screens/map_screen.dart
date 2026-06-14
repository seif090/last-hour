import 'package:flutter/material.dart';
import '../../../../core/theme/app_colors.dart';
import '../../../../core/theme/app_text_styles.dart';

class MapScreen extends StatelessWidget {
  const MapScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            _buildHeader(context),
            Expanded(
              child: Stack(
                children: [
                  Container(
                    color: AppColors.grey200,
                    child: Center(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            Icons.map_rounded,
                            size: 80,
                            color: AppColors.grey400,
                          ),
                          const SizedBox(height: 16),
                          Text(
                            'Map View',
                            style: theme.textTheme.titleMedium?.copyWith(
                              color: AppColors.grey500,
                            ),
                          ),
                          const SizedBox(height: 8),
                          Text(
                            'Google Maps will be integrated here',
                            style: AppTextStyles.caption,
                          ),
                        ],
                      ),
                    ),
                  ),
                  Positioned(
                    top: 16,
                    left: 16,
                    right: 16,
                    child: _buildSearchField(context),
                  ),
                  Positioned(
                    right: 16,
                    bottom: 100,
                    child: Column(
                      children: [
                        _buildMapButton(Icons.my_location_rounded, () {}),
                        const SizedBox(height: 8),
                        _buildMapButton(Icons.add_rounded, () {}),
                        const SizedBox(height: 8),
                        _buildMapButton(Icons.remove_rounded, () {}),
                      ],
                    ),
                  ),
                  Positioned(
                    bottom: 0,
                    left: 0,
                    right: 0,
                    child: _buildBottomSheet(context),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildHeader(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Row(
        children: [
          Text(
            'Nearby Stores',
            style: Theme.of(context).textTheme.titleLarge?.copyWith(
              fontWeight: FontWeight.bold,
            ),
          ),
          const Spacer(),
          TextButton.icon(
            onPressed: () {},
            icon: const Icon(Icons.list_rounded, size: 20),
            label: const Text('List'),
          ),
        ],
      ),
    );
  }

  Widget _buildSearchField(BuildContext context) {
    return Container(
      height: 44,
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha(26),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: TextField(
        decoration: InputDecoration(
          hintText: 'Search stores...',
          prefixIcon: const Icon(Icons.search_rounded, size: 20),
          border: InputBorder.none,
          enabledBorder: InputBorder.none,
          focusedBorder: InputBorder.none,
          contentPadding: const EdgeInsets.symmetric(vertical: 10),
        ),
      ),
    );
  }

  Widget _buildMapButton(IconData icon, VoidCallback onTap) {
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(12),
      elevation: 2,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(12),
          ),
          child: Icon(icon, size: 22, color: AppColors.grey700),
        ),
      ),
    );
  }

  Widget _buildBottomSheet(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha(26),
            blurRadius: 10,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Center(
            child: Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: AppColors.grey300,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),
          const SizedBox(height: 16),
          Text(
            'Stores Near You',
            style: theme.textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 12),
          SizedBox(
            height: 100,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: 5,
              separatorBuilder: (_, __) => const SizedBox(width: 12),
              itemBuilder: (_, index) {
                return _buildStoreMiniCard(index, theme);
              },
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStoreMiniCard(int index, ThemeData theme) {
    final colors = [
      AppColors.primary,
      AppColors.secondary,
      AppColors.tertiary,
      AppColors.discountRed,
      AppColors.success,
    ];
    return Container(
      width: 120,
      decoration: BoxDecoration(
        color: colors[index].withAlpha(26),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: colors[index],
              shape: BoxShape.circle,
            ),
            child: const Icon(Icons.store_rounded, color: Colors.white, size: 22),
          ),
          const SizedBox(height: 8),
          Text(
            'Store ${index + 1}',
            style: AppTextStyles.labelMedium.copyWith(
              fontWeight: FontWeight.w600,
            ),
          ),
          Text(
            '${(index + 1) * 0.5} km',
            style: AppTextStyles.caption.copyWith(fontSize: 10),
          ),
        ],
      ),
    );
  }
}
