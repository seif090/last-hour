class Store {
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

  const Store({
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
}
