import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/providers/core_providers.dart';
import '../../data/datasources/offer_remote_data_source.dart';
import '../../data/repositories/offer_repository_impl.dart';
import '../../domain/entities/offer.dart';
import '../../domain/repositories/offer_repository.dart';

final offerRemoteDataSourceProvider = Provider<OfferRemoteDataSource>((ref) {
  return OfferRemoteDataSource(ref.read(dioClientProvider));
});

final offerRepositoryProvider = Provider<OfferRepository>((ref) {
  return OfferRepositoryImpl(ref.read(offerRemoteDataSourceProvider));
});

final featuredOffersProvider = FutureProvider<List<Offer>>((ref) async {
  final result = await ref.read(offerRepositoryProvider).getFeaturedOffers();
  if (result.isSuccess) return result.data ?? [];
  throw result.error ?? 'Failed to load featured offers';
});

final nearbyOffersProvider = FutureProvider<List<Offer>>((ref) async {
  final result = await ref.read(offerRepositoryProvider).getNearbyOffers();
  if (result.isSuccess) return result.data ?? [];
  throw result.error ?? 'Failed to load nearby offers';
});

final offersByCategoryProvider = FutureProvider.family<List<Offer>, String>((ref, category) async {
  final result = await ref.read(offerRepositoryProvider).getOffersByCategory(category);
  if (result.isSuccess) return result.data ?? [];
  throw result.error ?? 'Failed to load offers';
});

final offerDetailsProvider = FutureProvider.family<Offer, String>((ref, offerId) async {
  final result = await ref.read(offerRepositoryProvider).getOfferDetails(offerId);
  if (result.isSuccess && result.data != null) return result.data!;
  throw result.error ?? 'Failed to load offer details';
});
