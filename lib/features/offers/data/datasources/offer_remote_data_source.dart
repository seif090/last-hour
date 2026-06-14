import '../../../../core/constants/api_constants.dart';
import '../../../../core/network/api_response.dart';
import '../../../../core/network/dio_client.dart';
import '../models/offer_model.dart';

class OfferRemoteDataSource {
  final DioClient _dioClient;

  OfferRemoteDataSource(this._dioClient);

  Future<ApiResponse<OfferModel>> getOfferById(String id) async {
    final response = await _dioClient.get('${ApiConstants.offers}/$id');
    return ApiResponse<OfferModel>.fromJson(
      response.data,
      fromJsonT: (json) => OfferModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<PaginatedResponse<OfferModel>> getOffers({
    int page = 1,
    int perPage = 20,
    String? category,
    double? latitude,
    double? longitude,
    double? radiusKm,
    String? search,
    String? sortBy,
  }) async {
    final queryParams = <String, dynamic>{
      'page': page,
      'per_page': perPage,
      if (category != null) 'category': category,
      if (latitude != null) 'latitude': latitude,
      if (longitude != null) 'longitude': longitude,
      if (radiusKm != null) 'radius_km': radiusKm,
      if (search != null) 'search': search,
      if (sortBy != null) 'sort_by': sortBy,
    };
    final response = await _dioClient.get(
      ApiConstants.offers,
      queryParameters: queryParams,
    );
    return PaginatedResponse<OfferModel>.fromJson(
      response.data,
      fromJsonT: (json) => OfferModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<PaginatedResponse<OfferModel>> getFeaturedOffers({
    int page = 1,
    int perPage = 10,
  }) async {
    final response = await _dioClient.get(
      '${ApiConstants.offers}/featured',
      queryParameters: {'page': page, 'per_page': perPage},
    );
    return PaginatedResponse<OfferModel>.fromJson(
      response.data,
      fromJsonT: (json) => OfferModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<PaginatedResponse<OfferModel>> getNearbyOffers({
    required double latitude,
    required double longitude,
    double radiusKm = 5,
    int page = 1,
    int perPage = 20,
  }) async {
    final response = await _dioClient.get(
      '${ApiConstants.offers}/nearby',
      queryParameters: {
        'latitude': latitude, 'longitude': longitude,
        'radius_km': radiusKm, 'page': page, 'per_page': perPage,
      },
    );
    return PaginatedResponse<OfferModel>.fromJson(
      response.data,
      fromJsonT: (json) => OfferModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<PaginatedResponse<OfferModel>> getOffersByStore(String storeId, {
    int page = 1,
    int perPage = 20,
  }) async {
    final response = await _dioClient.get(
      '${ApiConstants.stores}/$storeId/offers',
      queryParameters: {'page': page, 'per_page': perPage},
    );
    return PaginatedResponse<OfferModel>.fromJson(
      response.data,
      fromJsonT: (json) => OfferModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<OfferModel>> toggleFavorite(String offerId) async {
    final response = await _dioClient.post('${ApiConstants.offers}/$offerId/favorite');
    return ApiResponse<OfferModel>.fromJson(
      response.data,
      fromJsonT: (json) => OfferModel.fromJson(json as Map<String, dynamic>),
    );
  }
}
