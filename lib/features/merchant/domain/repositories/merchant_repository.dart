import '../../../../core/utils/result.dart';
import '../../../offers/domain/entities/offer.dart';
import '../entities/merchant.dart';
import '../../../orders/domain/entities/order.dart';

abstract class MerchantRepository {
  Future<Result<Merchant>> login({required String email, required String password});
  Future<Result<Merchant>> getDashboard();
  Future<Result<List<Offer>>> getOffers();
  Future<Result<Offer>> createOffer({
    required String title,
    required String description,
    required String category,
    required double originalPrice,
    required double discountPrice,
    required int quantity,
    String? imagePath,
    DateTime? pickupStart,
    DateTime? pickupEnd,
  });
  Future<Result<Offer>> updateOffer({
    required String offerId,
    String? title,
    String? description,
    double? originalPrice,
    double? discountPrice,
    int? quantity,
  });
  Future<Result<void>> toggleOfferActive(String offerId, bool isActive);
  Future<Result<List<Order>>> getOrders({String? status});
  Future<Result<Order>> updateOrderStatus({
    required String orderId,
    required String status,
  });
  Future<Result<Map<String, dynamic>>> getReports({String? period});
}
