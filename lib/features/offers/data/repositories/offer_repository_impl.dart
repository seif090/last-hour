import 'package:dio/dio.dart';
import '../../../../core/utils/result.dart';
import '../../domain/entities/offer.dart';
import '../../domain/repositories/offer_repository.dart';
import '../datasources/offer_remote_data_source.dart';

class OfferRepositoryImpl implements OfferRepository {
  final OfferRemoteDataSource _remoteDataSource;

  OfferRepositoryImpl(this._remoteDataSource);

  @override
  Future<Result<List<Offer>>> getFeaturedOffers() async {
    try {
      final response = await _remoteDataSource.getFeaturedOffers();
      return Result.success(
        response.items.map((model) => model.toEntity()).toList(),
      );
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<List<Offer>>> getOffersByCategory(String category) async {
    try {
      final response = await _remoteDataSource.getOffers(category: category);
      return Result.success(
        response.items.map((model) => model.toEntity()).toList(),
      );
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<List<Offer>>> getOffersByStore(String storeId) async {
    try {
      final response = await _remoteDataSource.getOffersByStore(storeId);
      return Result.success(
        response.items.map((model) => model.toEntity()).toList(),
      );
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<List<Offer>>> getNearbyOffers({double? latitude, double? longitude, double? radius}) async {
    try {
      final response = await _remoteDataSource.getNearbyOffers(
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
  Future<Result<Offer>> getOfferDetails(String offerId) async {
    try {
      final response = await _remoteDataSource.getOfferById(offerId);
      return Result.success(response.data!.toEntity());
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<List<Offer>>> searchOffers(String query) async {
    try {
      final response = await _remoteDataSource.getOffers(search: query);
      return Result.success(
        response.items.map((model) => model.toEntity()).toList(),
      );
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<void>> toggleFavorite(String offerId) async {
    try {
      await _remoteDataSource.toggleFavorite(offerId);
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
