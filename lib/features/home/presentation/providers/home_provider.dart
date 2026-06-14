import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/models/home_models.dart';

final homeCategoriesProvider = Provider<List<Category>>((ref) {
  return Category.mockCategories;
});

final featuredOffersProvider = Provider<List<HomeOffer>>((ref) {
  return HomeOffer.mockOffers;
});

final nearbyOffersProvider = Provider<List<HomeOffer>>((ref) {
  return HomeOffer.mockOffers.where((o) => o.distance <= 2.0).toList();
});

final selectedCategoryProvider = StateProvider<String?>((ref) => null);

final filteredOffersProvider = Provider<List<HomeOffer>>((ref) {
  final selectedCategory = ref.watch(selectedCategoryProvider);
  final offers = ref.watch(nearbyOffersProvider);
  if (selectedCategory == null) return offers;
  return offers.where((o) => o.category == selectedCategory).toList();
});
