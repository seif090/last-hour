import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../offers/domain/entities/offer.dart';
import '../../../offers/presentation/providers/offer_providers.dart';
import '../../data/models/home_models.dart';

final homeCategoriesProvider = Provider<List<Category>>((ref) {
  return Category.mockCategories;
});

final selectedCategoryProvider = StateProvider<String?>((ref) => null);

final homeFeaturedOffersProvider = FutureProvider<List<HomeOffer>>((ref) async {
  final offers = await ref.watch(featuredOffersProvider.future);
  return offers.map(_toHomeOffer).toList();
});

final homeNearbyOffersProvider = FutureProvider<List<HomeOffer>>((ref) async {
  final offers = await ref.watch(nearbyOffersProvider.future);
  return offers.map(_toHomeOffer).toList();
});

final filteredOffersProvider = FutureProvider<List<HomeOffer>>((ref) async {
  final selectedCategory = ref.watch(selectedCategoryProvider);
  final offers = await ref.watch(homeNearbyOffersProvider.future);
  if (selectedCategory == null) return offers;
  return offers.where((o) => o.category == selectedCategory).toList();
});

HomeOffer _toHomeOffer(Offer o) => HomeOffer(
  id: o.id,
  title: o.title,
  storeName: o.storeName,
  storeId: o.storeId,
  imageUrl: o.imageUrl ?? '',
  originalPrice: o.originalPrice,
  discountPrice: o.discountPrice,
  remainingQuantity: o.remainingQuantity,
  expiryTime: o.expiryTime,
  distance: o.distance,
  rating: o.rating,
  category: o.category,
);
