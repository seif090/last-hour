import 'package:dio/dio.dart';
import '../../../../core/utils/result.dart';
import '../../domain/entities/order.dart';
import '../../domain/repositories/order_repository.dart';
import '../datasources/order_remote_data_source.dart';

class OrderRepositoryImpl implements OrderRepository {
  final OrderRemoteDataSource _remoteDataSource;

  OrderRepositoryImpl(this._remoteDataSource);

  @override
  Future<Result<List<Order>>> getOrders({OrderStatus? status}) async {
    try {
      final statusStr = status != null ? _statusToString(status) : null;
      final response = await _remoteDataSource.getOrders(status: statusStr);
      return Result.success(
        response.items.map((model) => model.toEntity()).toList(),
      );
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<Order>> getOrderDetails(String orderId) async {
    try {
      final response = await _remoteDataSource.getOrderById(orderId);
      return Result.success(response.data!.toEntity());
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<Order>> createOrder({
    required String storeId,
    required List<Map<String, dynamic>> items,
    required String paymentMethod,
    bool isDelivery = false,
    String? deliveryAddress,
    String? couponCode,
    String? notes,
  }) async {
    try {
      final response = await _remoteDataSource.createOrder(
        storeId: storeId,
        items: items,
        paymentMethod: paymentMethod,
        isDelivery: isDelivery,
        deliveryAddress: deliveryAddress,
        couponCode: couponCode,
        notes: notes,
      );
      return Result.success(response.data!.toEntity());
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<void>> cancelOrder(String orderId) async {
    try {
      await _remoteDataSource.cancelOrder(orderId);
      return Result.success(null);
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<Order>> trackOrder(String orderId) async {
    return getOrderDetails(orderId);
  }

  String _statusToString(OrderStatus status) {
    switch (status) {
      case OrderStatus.pending: return 'pending';
      case OrderStatus.confirmed: return 'confirmed';
      case OrderStatus.preparing: return 'preparing';
      case OrderStatus.ready: return 'ready';
      case OrderStatus.pickedUp: return 'picked_up';
      case OrderStatus.delivered: return 'delivered';
      case OrderStatus.cancelled: return 'cancelled';
      case OrderStatus.rejected: return 'rejected';
    }
  }

  String _mapError(DioException e) {
    if (e.response?.data is Map) {
      return (e.response!.data as Map)['message'] as String? ?? e.message ?? 'Something went wrong';
    }
    return e.message ?? 'Something went wrong';
  }
}
