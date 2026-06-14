import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/providers/core_providers.dart';
import '../../data/datasources/cart_remote_data_source.dart';
import '../../data/repositories/cart_repository_impl.dart';
import '../../domain/entities/cart_item.dart';
import '../../domain/repositories/cart_repository.dart';

final cartRemoteDataSourceProvider = Provider<CartRemoteDataSource>((ref) {
  return CartRemoteDataSource(ref.read(dioClientProvider));
});

final cartRepositoryProvider = Provider<CartRepository>((ref) {
  return CartRepositoryImpl(ref.read(cartRemoteDataSourceProvider));
});

final cartItemsProvider = FutureProvider<List<CartItem>>((ref) async {
  final result = await ref.read(cartRepositoryProvider).getCartItems();
  if (result.isSuccess) return result.data ?? [];
  throw result.error ?? 'Failed to load cart';
});

final cartTotalProvider = Provider<double>((ref) {
  final cartAsync = ref.watch(cartItemsProvider);
  return cartAsync.when(
    data: (items) => items.fold(0, (sum, item) => sum + item.price * item.quantity),
    loading: () => 0,
    error: (_, __) => 0,
  );
});

final cartItemCountProvider = Provider<int>((ref) {
  final cartAsync = ref.watch(cartItemsProvider);
  return cartAsync.when(
    data: (items) => items.fold(0, (sum, item) => sum + item.quantity),
    loading: () => 0,
    error: (_, __) => 0,
  );
});
