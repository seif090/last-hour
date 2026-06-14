import 'package:dio/dio.dart';
import '../../../../core/utils/result.dart';
import '../../domain/entities/cart_item.dart';
import '../../domain/repositories/cart_repository.dart';
import '../datasources/cart_remote_data_source.dart';

class CartRepositoryImpl implements CartRepository {
  final CartRemoteDataSource _remoteDataSource;

  CartRepositoryImpl(this._remoteDataSource);

  @override
  Future<Result<List<CartItem>>> getCartItems() async {
    try {
      final models = await _remoteDataSource.getCart();
      return Result.success(models.map((m) => m.toEntity()).toList());
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<void>> addToCart({
    required String offerId,
    required int quantity,
  }) async {
    try {
      await _remoteDataSource.addToCart(offerId: offerId, quantity: quantity);
      return Result.success(null);
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<void>> updateQuantity({
    required String cartItemId,
    required int quantity,
  }) async {
    try {
      await _remoteDataSource.updateCartItem(itemId: cartItemId, quantity: quantity);
      return Result.success(null);
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<void>> removeFromCart(String cartItemId) async {
    try {
      await _remoteDataSource.removeFromCart(cartItemId);
      return Result.success(null);
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<void>> clearCart() async {
    try {
      await _remoteDataSource.clearCart();
      return Result.success(null);
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<double>> applyCoupon(String code) async {
    try {
      final response = await _remoteDataSource.validateCoupon(code);
      return Result.success(response.data ?? 0);
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<void>> removeCoupon() async {
    return Result.success(null);
  }

  String _mapError(DioException e) {
    if (e.response?.data is Map) {
      return (e.response!.data as Map)['message'] as String? ?? e.message ?? 'Something went wrong';
    }
    return e.message ?? 'Something went wrong';
  }
}
