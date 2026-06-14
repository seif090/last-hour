import '../../../../core/utils/result.dart';
import '../entities/store.dart';

abstract class StoreRepository {
  Future<Result<List<Store>>> getNearbyStores({double? latitude, double? longitude, double? radius});
  Future<Result<Store>> getStoreDetails(String storeId);
  Future<Result<List<Store>>> searchStores(String query);
  Future<Result<List<Store>>> getFavoriteStores();
  Future<Result<void>> toggleFavorite(String storeId);
}
