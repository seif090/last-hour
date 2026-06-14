import '../../../../core/utils/result.dart';
import '../entities/offer.dart';

abstract class OfferRepository {
  Future<Result<List<Offer>>> getFeaturedOffers();
  Future<Result<List<Offer>>> getOffersByCategory(String category);
  Future<Result<List<Offer>>> getOffersByStore(String storeId);
  Future<Result<List<Offer>>> getNearbyOffers({double? latitude, double? longitude, double? radius});
  Future<Result<Offer>> getOfferDetails(String offerId);
  Future<Result<List<Offer>>> searchOffers(String query);
  Future<Result<void>> toggleFavorite(String offerId);
}
