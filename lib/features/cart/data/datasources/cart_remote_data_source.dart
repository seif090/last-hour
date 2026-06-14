import '../../../../core/constants/api_constants.dart';
import '../../../../core/network/api_response.dart';
import '../../../../core/network/dio_client.dart';
import '../models/cart_item_model.dart';

class CartRemoteDataSource {
  final DioClient _dioClient;

  CartRemoteDataSource(this._dioClient);

  Future<List<CartItemModel>> getCart() async {
    final response = await _dioClient.get(ApiConstants.cart);
    final apiResponse = ApiResponse<List<CartItemModel>>.fromJson(
      response.data,
      fromJsonT: (json) => (json as List).map((e) => CartItemModel.fromJson(e as Map<String, dynamic>)).toList(),
    );
    return apiResponse.data ?? [];
  }

  Future<ApiResponse<CartItemModel>> addToCart({
    required String offerId,
    required int quantity,
  }) async {
    final response = await _dioClient.post(ApiConstants.cart, data: {
      'offer_id': offerId,
      'quantity': quantity,
    });
    return ApiResponse<CartItemModel>.fromJson(
      response.data,
      fromJsonT: (json) => CartItemModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<ApiResponse<CartItemModel>> updateCartItem({
    required String itemId,
    required int quantity,
  }) async {
    final response = await _dioClient.put(
      '${ApiConstants.cart}/$itemId',
      data: {'quantity': quantity},
    );
    return ApiResponse<CartItemModel>.fromJson(
      response.data,
      fromJsonT: (json) => CartItemModel.fromJson(json as Map<String, dynamic>),
    );
  }

  Future<void> removeFromCart(String itemId) async {
    await _dioClient.delete('${ApiConstants.cart}/$itemId');
  }

  Future<void> clearCart() async {
    await _dioClient.delete(ApiConstants.cart);
  }

  Future<ApiResponse<double>> validateCoupon(String code) async {
    final response = await _dioClient.post('${ApiConstants.cart}/validate-coupon', data: {
      'code': code,
    });
    return ApiResponse<double>.fromJson(
      response.data,
      fromJsonT: (json) => (json as num).toDouble(),
    );
  }
}
