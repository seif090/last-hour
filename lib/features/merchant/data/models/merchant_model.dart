import '../../domain/entities/merchant.dart';

class MerchantModel {
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
  final String createdAt;
  final String? accessToken;
  final String? refreshToken;

  const MerchantModel({
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
    this.accessToken,
    this.refreshToken,
  });

  factory MerchantModel.fromJson(Map<String, dynamic> json) {
    return MerchantModel(
      id: json['id']?.toString() ?? '',
      name: json['name'] as String? ?? '',
      email: json['email'] as String? ?? '',
      phone: json['phone'] as String?,
      logoUrl: json['logo_url'] as String?,
      coverImageUrl: json['cover_image_url'] as String?,
      storeName: json['store_name'] as String? ?? '',
      storeCategory: json['store_category'] as String? ?? '',
      storeDescription: json['store_description'] as String?,
      storeAddress: json['store_address'] as String? ?? '',
      storeLatitude: (json['store_latitude'] as num?)?.toDouble(),
      storeLongitude: (json['store_longitude'] as num?)?.toDouble(),
      isOnline: json['is_online'] as bool? ?? false,
      activeOfferCount: json['active_offer_count'] as int? ?? 0,
      todayOrderCount: json['today_order_count'] as int? ?? 0,
      todayRevenue: (json['today_revenue'] as num?)?.toDouble() ?? 0,
      rating: (json['rating'] as num?)?.toDouble() ?? 0,
      createdAt: json['created_at'] as String? ?? '',
      accessToken: json['access_token'] as String?,
      refreshToken: json['refresh_token'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
    'email': email,
    'phone': phone,
    'logo_url': logoUrl,
    'cover_image_url': coverImageUrl,
    'store_name': storeName,
    'store_category': storeCategory,
    'store_description': storeDescription,
    'store_address': storeAddress,
    'store_latitude': storeLatitude,
    'store_longitude': storeLongitude,
    'is_online': isOnline,
    'active_offer_count': activeOfferCount,
    'today_order_count': todayOrderCount,
    'today_revenue': todayRevenue,
    'rating': rating,
    'created_at': createdAt,
    'access_token': accessToken,
    'refresh_token': refreshToken,
  };

  Merchant toEntity() => Merchant(
    id: id, name: name, email: email, phone: phone,
    logoUrl: logoUrl, coverImageUrl: coverImageUrl,
    storeName: storeName, storeCategory: storeCategory,
    storeDescription: storeDescription, storeAddress: storeAddress,
    storeLatitude: storeLatitude, storeLongitude: storeLongitude,
    isOnline: isOnline, activeOfferCount: activeOfferCount,
    todayOrderCount: todayOrderCount, todayRevenue: todayRevenue,
    rating: rating,
    createdAt: DateTime.tryParse(createdAt) ?? DateTime.now(),
  );
}
