import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/providers/core_providers.dart';
import '../../data/datasources/store_remote_data_source.dart';
import '../../data/repositories/store_repository_impl.dart';
import '../../domain/entities/store.dart';
import '../../domain/repositories/store_repository.dart';

final storeRemoteDataSourceProvider = Provider<StoreRemoteDataSource>((ref) {
  return StoreRemoteDataSource(ref.read(dioClientProvider));
});

final storeRepositoryProvider = Provider<StoreRepository>((ref) {
  return StoreRepositoryImpl(ref.read(storeRemoteDataSourceProvider));
});

final nearbyStoresProvider = FutureProvider<List<Store>>((ref) async {
  final result = await ref.read(storeRepositoryProvider).getNearbyStores();
  if (result.isSuccess) return result.data ?? [];
  throw result.error ?? 'Failed to load nearby stores';
});

final storeDetailsProvider = FutureProvider.family<Store, String>((ref, storeId) async {
  final result = await ref.read(storeRepositoryProvider).getStoreDetails(storeId);
  if (result.isSuccess && result.data != null) return result.data!;
  throw result.error ?? 'Failed to load store details';
});
