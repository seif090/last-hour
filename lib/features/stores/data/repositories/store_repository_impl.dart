import 'package:dio/dio.dart';
import '../../../../core/utils/result.dart';
import '../../domain/entities/store.dart';
import '../../domain/repositories/store_repository.dart';
import '../datasources/store_remote_data_source.dart';

class StoreRepositoryImpl implements StoreRepository {
  final StoreRemoteDataSource _remoteDataSource;

  StoreRepositoryImpl(this._remoteDataSource);

  @override
  Future<Result<List<Store>>> getNearbyStores({double? latitude, double? longitude, double? radius}) async {
    try {
      final response = await _remoteDataSource.getNearbyStores(
        latitude: latitude ?? 0,
        longitude: longitude ?? 0,
        radiusKm: radius ?? 5,
      );
      return Result.success(
        response.items.map((model) => model.toEntity()).toList(),
      );
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<Store>> getStoreDetails(String storeId) async {
    try {
      final response = await _remoteDataSource.getStoreById(storeId);
      return Result.success(response.data!.toEntity());
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<List<Store>>> searchStores(String query) async {
    try {
      final response = await _remoteDataSource.getStores(search: query);
      return Result.success(
        response.items.map((model) => model.toEntity()).toList(),
      );
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<List<Store>>> getFavoriteStores() async {
    try {
      final response = await _remoteDataSource.getStores();
      final favorites = response.items
          .where((model) => model.isFavorite)
          .map((model) => model.toEntity())
          .toList();
      return Result.success(favorites);
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<void>> toggleFavorite(String storeId) async {
    try {
      await _remoteDataSource.toggleFavorite(storeId);
      return Result.success(null);
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  String _mapError(DioException e) {
    if (e.response?.data is Map) {
      return (e.response!.data as Map)['message'] as String? ?? e.message ?? 'Something went wrong';
    }
    return e.message ?? 'Something went wrong';
  }
}
