class Merchant {
  final String id;
  final String name;
  final String email;
  final String? phone;
  final String? logoUrl;
  final String? coverImageUrl;
  final String storeName;
  final String storeCategory;
  final String? storeDescription;
  final String storeAddress;
  final double? storeLatitude;
  final double? storeLongitude;
  final bool isOnline;
  final int activeOfferCount;
  final int todayOrderCount;
  final double todayRevenue;
  final double rating;
  final DateTime createdAt;

  const Merchant({
    required this.id,
    required this.name,
    required this.email,
    this.phone,
    this.logoUrl,
    this.coverImageUrl,
    required this.storeName,
    required this.storeCategory,
    this.storeDescription,
    required this.storeAddress,
    this.storeLatitude,
    this.storeLongitude,
    this.isOnline = false,
    this.activeOfferCount = 0,
    this.todayOrderCount = 0,
    this.todayRevenue = 0,
    this.rating = 0,
    required this.createdAt,
  });
}
