import '../../../../core/utils/result.dart';
import '../entities/cart_item.dart';

abstract class CartRepository {
  Future<Result<List<CartItem>>> getCartItems();
  Future<Result<void>> addToCart({
    required String offerId,
    required int quantity,
  });
  Future<Result<void>> updateQuantity({
    required String cartItemId,
    required int quantity,
  });
  Future<Result<void>> removeFromCart(String cartItemId);
  Future<Result<void>> clearCart();
  Future<Result<double>> applyCoupon(String code);
  Future<Result<void>> removeCoupon();
}
