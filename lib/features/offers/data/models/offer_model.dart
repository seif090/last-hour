import '../../domain/entities/offer.dart';

class OfferModel {
  final String id;
  final String title;
  final String description;
  final String storeId;
  final String storeName;
  final String? storeLogoUrl;
  final double originalPrice;
  final double discountPrice;
  final int remainingQuantity;
  final int originalQuantity;
  final String expiryTime;
  final String category;
  final String? imageUrl;
  final List<String> imageUrls;
  final double distance;
  final double rating;
  final int reviewCount;
  final bool isFavorite;
  final bool isActive;
  final String createdAt;

  const OfferModel({
    required this.id,
    required this.title,
    this.description = '',
    required this.storeId,
    required this.storeName,
    this.storeLogoUrl,
    required this.originalPrice,
    required this.discountPrice,
    required this.remainingQuantity,
    this.originalQuantity = 0,
    required this.expiryTime,
    required this.category,
    this.imageUrl,
    this.imageUrls = const [],
    this.distance = 0,
    this.rating = 0,
    this.reviewCount = 0,
    this.isFavorite = false,
    this.isActive = true,
    required this.createdAt,
  });

  factory OfferModel.fromJson(Map<String, dynamic> json) {
    return OfferModel(
      id: json['id']?.toString() ?? '',
      title: json['title'] as String? ?? '',
      description: json['description'] as String? ?? '',
      storeId: json['store_id']?.toString() ?? '',
      storeName: json['store_name'] as String? ?? '',
      storeLogoUrl: json['store_logo_url'] as String?,
      originalPrice: (json['original_price'] as num?)?.toDouble() ?? 0,
      discountPrice: (json['discount_price'] as num?)?.toDouble() ?? 0,
      remainingQuantity: json['remaining_quantity'] as int? ?? 0,
      originalQuantity: json['original_quantity'] as int? ?? 0,
      expiryTime: json['expiry_time'] as String? ?? '',
      category: json['category'] as String? ?? '',
      imageUrl: json['image_url'] as String?,
      imageUrls: (json['image_urls'] as List<dynamic>?)?.cast<String>() ?? [],
      distance: (json['distance'] as num?)?.toDouble() ?? 0,
      rating: (json['rating'] as num?)?.toDouble() ?? 0,
      reviewCount: json['review_count'] as int? ?? 0,
      isFavorite: json['is_favorite'] as bool? ?? false,
      isActive: json['is_active'] as bool? ?? true,
      createdAt: json['created_at'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'title': title,
      'description': description,
      'store_id': storeId,
      'store_name': storeName,
      'store_logo_url': storeLogoUrl,
      'original_price': originalPrice,
      'discount_price': discountPrice,
      'remaining_quantity': remainingQuantity,
      'original_quantity': originalQuantity,
      'expiry_time': expiryTime,
      'category': category,
      'image_url': imageUrl,
      'image_urls': imageUrls,
      'distance': distance,
      'rating': rating,
      'review_count': reviewCount,
      'is_favorite': isFavorite,
      'is_active': isActive,
      'created_at': createdAt,
    };
  }

  Offer toEntity() {
    return Offer(
      id: id,
      title: title,
      description: description,
      storeId: storeId,
      storeName: storeName,
      storeLogoUrl: storeLogoUrl,
      originalPrice: originalPrice,
      discountPrice: discountPrice,
      remainingQuantity: remainingQuantity,
      originalQuantity: originalQuantity,
      expiryTime: DateTime.tryParse(expiryTime) ?? DateTime.now(),
      category: category,
      imageUrl: imageUrl,
      imageUrls: imageUrls,
      distance: distance,
      rating: rating,
      reviewCount: reviewCount,
      isFavorite: isFavorite,
      isActive: isActive,
      createdAt: DateTime.tryParse(createdAt) ?? DateTime.now(),
    );
  }

  factory OfferModel.fromEntity(Offer entity) {
    return OfferModel(
      id: entity.id,
      title: entity.title,
      description: entity.description,
      storeId: entity.storeId,
      storeName: entity.storeName,
      storeLogoUrl: entity.storeLogoUrl,
      originalPrice: entity.originalPrice,
      discountPrice: entity.discountPrice,
      remainingQuantity: entity.remainingQuantity,
      originalQuantity: entity.originalQuantity,
      expiryTime: entity.expiryTime.toIso8601String(),
      category: entity.category,
      imageUrl: entity.imageUrl,
      imageUrls: entity.imageUrls,
      distance: entity.distance,
      rating: entity.rating,
      reviewCount: entity.reviewCount,
      isFavorite: entity.isFavorite,
      isActive: entity.isActive,
      createdAt: entity.createdAt.toIso8601String(),
    );
  }
}
