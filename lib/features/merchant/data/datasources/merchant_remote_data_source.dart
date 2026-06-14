import 'dart:io';
import 'package:dio/dio.dart';
import '../../../../core/constants/api_constants.dart';
import '../../../../core/network/api_response.dart';
import '../../../../core/network/dio_client.dart';
import '../../../offers/data/models/offer_model.dart' as merchant_offer;
import '../../../orders/data/models/order_model.dart' as merchant_order;
import '../models/merchant_model.dart';

class MerchantRemoteDataSource {
  final DioClient _dioClient;

  MerchantRemoteDataSource(this._dioClient);

  Future<ApiResponse<MerchantModel>> login({
    required String email,
    required String password,
  }) async {
    final response = await _dioClient.post(ApiConstants.merchantLogin, data: {
      'email': email,
      'password': password,
    });
    return ApiResponse<MerchantModel>.fromJson(
      response.data,
      fromJsonT: (json) => MerchantModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<MerchantModel>> getProfile() async {
    final response = await _dioClient.get('${ApiConstants.baseUrl}/merchant/profile');
    return ApiResponse<MerchantModel>.fromJson(
      response.data,
      fromJsonT: (json) => MerchantModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<MerchantModel>> getDashboard() async {
    final response = await _dioClient.get(ApiConstants.merchantDashboard);
    return ApiResponse<MerchantModel>.fromJson(
      response.data,
      fromJsonT: (json) => MerchantModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<PaginatedResponse<merchant_offer.OfferModel>> getOffers({
    int page = 1,
    int perPage = 20,
    String? status,
  }) async {
    final queryParams = <String, dynamic>{
      'page': page,
      'per_page': perPage,
      if (status != null) 'status': status,
    };
    final response = await _dioClient.get(
      ApiConstants.merchantOffers,
      queryParameters: queryParams,
    );
    return PaginatedResponse<merchant_offer.OfferModel>.fromJson(
      response.data,
      fromJsonT: (json) => merchant_offer.OfferModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<merchant_offer.OfferModel>> createOffer({
    required String title,
    required String description,
    required String category,
    required double originalPrice,
    required double discountPrice,
    required int quantity,
    File? image,
    String? pickupStart,
    String? pickupEnd,
  }) async {
    final formData = FormData.fromMap({
      'title': title,
      'description': description,
      'category': category,
      'original_price': originalPrice,
      'discount_price': discountPrice,
      'quantity': quantity,
      if (pickupStart != null) 'pickup_start': pickupStart,
      if (pickupEnd != null) 'pickup_end': pickupEnd,
      if (image != null) 'image': await MultipartFile.fromFile(image.path, filename: image.path.split('/').last),
    });
    final response = await _dioClient.post(
      ApiConstants.merchantOffers,
      data: formData,
    );
    return ApiResponse<merchant_offer.OfferModel>.fromJson(
      response.data,
      fromJsonT: (json) => merchant_offer.OfferModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<merchant_offer.OfferModel>> updateOffer({
    required String offerId,
    String? title,
    String? description,
    double? originalPrice,
    double? discountPrice,
    int? quantity,
    File? image,
  }) async {
    final formData = FormData.fromMap({
      if (title != null) 'title': title,
      if (description != null) 'description': description,
      if (originalPrice != null) 'original_price': originalPrice,
      if (discountPrice != null) 'discount_price': discountPrice,
      if (quantity != null) 'quantity': quantity,
      if (image != null) 'image': await MultipartFile.fromFile(image.path, filename: image.path.split('/').last),
    });
    final response = await _dioClient.put(
      '${ApiConstants.merchantOffers}/$offerId',
      data: formData,
    );
    return ApiResponse<merchant_offer.OfferModel>.fromJson(
      response.data,
      fromJsonT: (json) => merchant_offer.OfferModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<void>> toggleOfferActive(String offerId, bool isActive) async {
    final response = await _dioClient.patch(
      '${ApiConstants.merchantOffers}/$offerId',
      data: {'is_active': isActive},
    );
    return ApiResponse<void>.fromJson(response.data, fromJsonT: (_) {});
  }

  Future<PaginatedResponse<merchant_order.OrderModel>> getOrders({
    int page = 1,
    int perPage = 20,
    String? status,
  }) async {
    final queryParams = <String, dynamic>{
      'page': page,
      'per_page': perPage,
      if (status != null) 'status': status,
    };
    final response = await _dioClient.get(
      ApiConstants.merchantOrders,
      queryParameters: queryParams,
    );
    return PaginatedResponse<merchant_order.OrderModel>.fromJson(
      response.data,
      fromJsonT: (json) => merchant_order.OrderModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<merchant_order.OrderModel>> updateOrderStatus({
    required String orderId,
    required String status,
  }) async {
    final response = await _dioClient.patch(
      '${ApiConstants.merchantOrders}/$orderId',
      data: {'status': status},
    );
    return ApiResponse<merchant_order.OrderModel>.fromJson(
      response.data,
      fromJsonT: (json) => merchant_order.OrderModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<Map<String, dynamic>> getReports({String? period}) async {
    final queryParams = <String, dynamic>{
      if (period != null) 'period': period,
    };
    final response = await _dioClient.get(
      ApiConstants.merchantReports,
      queryParameters: queryParams,
    );
    final apiResponse = ApiResponse<Map<String, dynamic>>.fromJson(
      response.data,
      fromJsonT: (json) => json as Map<String, dynamic>,
    );
    return apiResponse.data ?? {};
  }
}
