import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/providers/core_providers.dart';
import '../../data/datasources/order_remote_data_source.dart';
import '../../data/repositories/order_repository_impl.dart';
import '../../domain/entities/order.dart';
import '../../domain/repositories/order_repository.dart';

final orderRemoteDataSourceProvider = Provider<OrderRemoteDataSource>((ref) {
  return OrderRemoteDataSource(ref.read(dioClientProvider));
});

final orderRepositoryProvider = Provider<OrderRepository>((ref) {
  return OrderRepositoryImpl(ref.read(orderRemoteDataSourceProvider));
});

final ordersProvider = FutureProvider<List<Order>>((ref) async {
  final result = await ref.read(orderRepositoryProvider).getOrders();
  if (result.isSuccess) return result.data ?? [];
  throw result.error ?? 'Failed to load orders';
});

final ordersByStatusProvider = FutureProvider.family<List<Order>, OrderStatus?>((ref, status) async {
  final result = await ref.read(orderRepositoryProvider).getOrders(status: status);
  if (result.isSuccess) return result.data ?? [];
  throw result.error ?? 'Failed to load orders';
});

final orderDetailsProvider = FutureProvider.family<Order, String>((ref, orderId) async {
  final result = await ref.read(orderRepositoryProvider).getOrderDetails(orderId);
  if (result.isSuccess && result.data != null) return result.data!;
  throw result.error ?? 'Failed to load order details';
});
