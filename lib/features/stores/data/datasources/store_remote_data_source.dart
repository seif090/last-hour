import '../../../../core/constants/api_constants.dart';
import '../../../../core/network/api_response.dart';
import '../../../../core/network/dio_client.dart';
import '../models/store_model.dart';

class StoreRemoteDataSource {
  final DioClient _dioClient;

  StoreRemoteDataSource(this._dioClient);

  Future<ApiResponse<StoreModel>> getStoreById(String id) async {
    final response = await _dioClient.get('${ApiConstants.stores}/$id');
    return ApiResponse<StoreModel>.fromJson(
      response.data,
      fromJsonT: (json) => StoreModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<PaginatedResponse<StoreModel>> getStores({
    int page = 1,
    int perPage = 20,
    String? category,
    double? latitude,
    double? longitude,
    double? radiusKm,
    String? search,
  }) async {
    final queryParams = <String, dynamic>{
      'page': page,
      'per_page': perPage,
      if (category != null) 'category': category,
      if (latitude != null) 'latitude': latitude,
      if (longitude != null) 'longitude': longitude,
      if (radiusKm != null) 'radius_km': radiusKm,
      if (search != null) 'search': search,
    };
    final response = await _dioClient.get(
      ApiConstants.stores,
      queryParameters: queryParams,
    );
    return PaginatedResponse<StoreModel>.fromJson(
      response.data,
      fromJsonT: (json) => StoreModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<PaginatedResponse<StoreModel>> getNearbyStores({
    required double latitude,
    required double longitude,
    double radiusKm = 5,
    int page = 1,
    int perPage = 20,
  }) async {
    final response = await _dioClient.get(
      '${ApiConstants.stores}/nearby',
      queryParameters: {
        'latitude': latitude, 'longitude': longitude,
        'radius_km': radiusKm, 'page': page, 'per_page': perPage,
      },
    );
    return PaginatedResponse<StoreModel>.fromJson(
      response.data,
      fromJsonT: (json) => StoreModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<void>> toggleFavorite(String storeId) async {
    final response = await _dioClient.post('${ApiConstants.stores}/$storeId/favorite');
    return ApiResponse<void>.fromJson(response.data, fromJsonT: (_) {});
  }

  Future<List<String>> getCategories() async {
    final response = await _dioClient.get('${ApiConstants.stores}/categories');
    final apiResponse = ApiResponse<List<String>>.fromJson(
      response.data,
      fromJsonT: (json) => (json as List).cast<String>(),
    );
    return apiResponse.data ?? [];
  }
}
