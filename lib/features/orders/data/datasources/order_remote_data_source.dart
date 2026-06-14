import '../../../../core/constants/api_constants.dart';
import '../../../../core/network/api_response.dart';
import '../../../../core/network/dio_client.dart';
import '../models/order_model.dart';

class OrderRemoteDataSource {
  final DioClient _dioClient;

  OrderRemoteDataSource(this._dioClient);

  Future<ApiResponse<OrderModel>> getOrderById(String id) async {
    final response = await _dioClient.get('${ApiConstants.orders}/$id');
    return ApiResponse<OrderModel>.fromJson(
      response.data,
      fromJsonT: (json) => OrderModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<PaginatedResponse<OrderModel>> getOrders({
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
      ApiConstants.orders,
      queryParameters: queryParams,
    );
    return PaginatedResponse<OrderModel>.fromJson(
      response.data,
      fromJsonT: (json) => OrderModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<OrderModel>> createOrder({
    required String storeId,
    required List<Map<String, dynamic>> items,
    required String paymentMethod,
    String? couponCode,
    bool isDelivery = false,
    String? deliveryAddress,
    String? notes,
  }) async {
    final response = await _dioClient.post(ApiConstants.orders, data: {
      'store_id': storeId,
      'items': items,
      'payment_method': paymentMethod,
      if (couponCode != null) 'coupon_code': couponCode,
      'is_delivery': isDelivery,
      if (deliveryAddress != null) 'delivery_address': deliveryAddress,
      if (notes != null) 'notes': notes,
    });
    return ApiResponse<OrderModel>.fromJson(
      response.data,
      fromJsonT: (json) => OrderModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<OrderModel>> cancelOrder(String orderId) async {
    final response = await _dioClient.post('${ApiConstants.orders}/$orderId/cancel');
    return ApiResponse<OrderModel>.fromJson(
      response.data,
      fromJsonT: (json) => OrderModel.fromJson(json as Map<String, dynamic>),
    );
  }
}
