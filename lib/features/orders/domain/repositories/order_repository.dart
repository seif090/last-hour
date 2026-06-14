import '../../../../core/utils/result.dart';
import '../entities/order.dart';

abstract class OrderRepository {
  Future<Result<List<Order>>> getOrders({OrderStatus? status});
  Future<Result<Order>> getOrderDetails(String orderId);
  Future<Result<Order>> createOrder({
    required String storeId,
    required List<Map<String, dynamic>> items,
    required String paymentMethod,
    bool isDelivery = false,
    String? deliveryAddress,
    String? couponCode,
    String? notes,
  });
  Future<Result<void>> cancelOrder(String orderId);
  Future<Result<Order>> trackOrder(String orderId);
}
