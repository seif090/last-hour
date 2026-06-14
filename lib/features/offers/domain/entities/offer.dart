class Offer {
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
  final DateTime expiryTime;
  final String category;
  final String? imageUrl;
  final List<String> imageUrls;
  final double distance;
  final double rating;
  final int reviewCount;
  final bool isFavorite;
  final bool isActive;
  final DateTime createdAt;

  const Offer({
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

  double get discountPercentage => originalPrice > 0
      ? ((originalPrice - discountPrice) / originalPrice * 100)
      : 0;

  bool get isExpired => DateTime.now().isAfter(expiryTime);

  bool get isSoldOut => remainingQuantity <= 0;
}
