import '../../domain/entities/store.dart';

class StoreModel {
  final String id;
  final String name;
  final String? description;
  final String? coverImageUrl;
  final String? logoUrl;
  final String category;
  final double rating;
  final int reviewCount;
  final double latitude;
  final double longitude;
  final String address;
  final String? phone;
  final String? email;
  final String? website;
  final bool isOpen;
  final String? openingHours;
  final String? closingHours;
  final double distance;
  final int activeOfferCount;
  final bool isFavorite;
  final List<String> acceptedPaymentMethods;

  const StoreModel({
    required this.id,
    required this.name,
    this.description,
    this.coverImageUrl,
    this.logoUrl,
    required this.category,
    this.rating = 0,
    this.reviewCount = 0,
    required this.latitude,
    required this.longitude,
    required this.address,
    this.phone,
    this.email,
    this.website,
    this.isOpen = false,
    this.openingHours,
    this.closingHours,
    this.distance = 0,
    this.activeOfferCount = 0,
    this.isFavorite = false,
    this.acceptedPaymentMethods = const [],
  });

  factory StoreModel.fromJson(Map<String, dynamic> json) {
    return StoreModel(
      id: json['id']?.toString() ?? '',
      name: json['name'] as String? ?? '',
      description: json['description'] as String?,
      coverImageUrl: json['cover_image_url'] as String?,
      logoUrl: json['logo_url'] as String?,
      category: json['category'] as String? ?? '',
      rating: (json['rating'] as num?)?.toDouble() ?? 0,
      reviewCount: json['review_count'] as int? ?? 0,
      latitude: (json['latitude'] as num?)?.toDouble() ?? 0,
      longitude: (json['longitude'] as num?)?.toDouble() ?? 0,
      address: json['address'] as String? ?? '',
      phone: json['phone'] as String?,
      email: json['email'] as String?,
      website: json['website'] as String?,
      isOpen: json['is_open'] as bool? ?? false,
      openingHours: json['opening_hours'] as String?,
      closingHours: json['closing_hours'] as String?,
      distance: (json['distance'] as num?)?.toDouble() ?? 0,
      activeOfferCount: json['active_offer_count'] as int? ?? 0,
      isFavorite: json['is_favorite'] as bool? ?? false,
      acceptedPaymentMethods: (json['accepted_payment_methods'] as List<dynamic>?)?.cast<String>() ?? [],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'description': description,
      'cover_image_url': coverImageUrl,
      'logo_url': logoUrl,
      'category': category,
      'rating': rating,
      'review_count': reviewCount,
      'latitude': latitude,
      'longitude': longitude,
      'address': address,
      'phone': phone,
      'email': email,
      'website': website,
      'is_open': isOpen,
      'opening_hours': openingHours,
      'closing_hours': closingHours,
      'distance': distance,
      'active_offer_count': activeOfferCount,
      'is_favorite': isFavorite,
      'accepted_payment_methods': acceptedPaymentMethods,
    };
  }

  Store toEntity() {
    return Store(
      id: id,
      name: name,
      description: description,
      coverImageUrl: coverImageUrl,
      logoUrl: logoUrl,
      category: category,
      rating: rating,
      reviewCount: reviewCount,
      latitude: latitude,
      longitude: longitude,
      address: address,
      phone: phone,
      email: email,
      website: website,
      isOpen: isOpen,
      openingHours: openingHours,
      closingHours: closingHours,
      distance: distance,
      activeOfferCount: activeOfferCount,
      isFavorite: isFavorite,
      acceptedPaymentMethods: acceptedPaymentMethods,
    );
  }

  factory StoreModel.fromEntity(Store entity) {
    return StoreModel(
      id: entity.id,
      name: entity.name,
      description: entity.description,
      coverImageUrl: entity.coverImageUrl,
      logoUrl: entity.logoUrl,
      category: entity.category,
      rating: entity.rating,
      reviewCount: entity.reviewCount,
      latitude: entity.latitude,
      longitude: entity.longitude,
      address: entity.address,
      phone: entity.phone,
      email: entity.email,
      website: entity.website,
      isOpen: entity.isOpen,
      openingHours: entity.openingHours,
      closingHours: entity.closingHours,
      distance: entity.distance,
      activeOfferCount: entity.activeOfferCount,
      isFavorite: entity.isFavorite,
      acceptedPaymentMethods: entity.acceptedPaymentMethods,
    );
  }
}
