import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';

class RatingDisplay extends StatelessWidget {
  final double rating;
  final int? reviewCount;
  final double starSize;
  final bool showCount;

  const RatingDisplay({
    super.key,
    required this.rating,
    this.reviewCount,
    this.starSize = 16,
    this.showCount = true,
  });

  @override
  Widget build(BuildContext context) {
    final fullStars = rating.floor();
    final hasHalfStar = rating - fullStars >= 0.3;
    final emptyStars = 5 - fullStars - (hasHalfStar ? 1 : 0);

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        ...List.generate(fullStars, (_) => _buildStar(Icons.star, AppColors.secondary)),
        if (hasHalfStar) _buildStar(Icons.star_half, AppColors.secondary),
        ...List.generate(emptyStars, (_) => _buildStar(Icons.star_border, AppColors.grey300)),
        const SizedBox(width: 4),
        Text(
          rating.toStringAsFixed(1),
          style: TextStyle(
            fontSize: starSize * 0.875,
            fontWeight: FontWeight.w600,
            color: AppColors.grey800,
          ),
        ),
        if (showCount && reviewCount != null) ...[
          const SizedBox(width: 2),
          Text(
            '($reviewCount)',
            style: TextStyle(
              fontSize: starSize * 0.75,
              color: AppColors.grey500,
            ),
          ),
        ],
      ],
    );
  }

  Widget _buildStar(IconData icon, Color color) {
    return Padding(
      padding: const EdgeInsets.only(right: 1),
      child: Icon(icon, size: starSize, color: color),
    );
  }
}
