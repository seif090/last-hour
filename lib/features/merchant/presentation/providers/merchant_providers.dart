import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/providers/core_providers.dart';
import '../../../offers/domain/entities/offer.dart';
import '../../../orders/domain/entities/order.dart';
import '../../data/datasources/merchant_remote_data_source.dart';
import '../../data/repositories/merchant_repository_impl.dart';
import '../../domain/entities/merchant.dart';
import '../../domain/repositories/merchant_repository.dart';

final merchantRemoteDataSourceProvider = Provider<MerchantRemoteDataSource>((ref) {
  return MerchantRemoteDataSource(ref.read(dioClientProvider));
});

final merchantRepositoryProvider = Provider<MerchantRepository>((ref) {
  return MerchantRepositoryImpl(ref.read(merchantRemoteDataSourceProvider));
});

final merchantAuthProvider = StateNotifierProvider<MerchantAuthNotifier, MerchantAuthState>((ref) {
  return MerchantAuthNotifier(ref.read(merchantRepositoryProvider));
});

class MerchantAuthState {
  final bool isLoading;
  final String? error;
  final Merchant? merchant;
  final bool isAuthenticated;

  const MerchantAuthState({
    this.isLoading = false,
    this.error,
    this.merchant,
    this.isAuthenticated = false,
  });

  MerchantAuthState copyWith({
    bool? isLoading,
    String? error,
    Merchant? merchant,
    bool? isAuthenticated,
  }) {
    return MerchantAuthState(
      isLoading: isLoading ?? this.isLoading,
      error: error,
      merchant: merchant ?? this.merchant,
      isAuthenticated: isAuthenticated ?? this.isAuthenticated,
    );
  }
}

class MerchantAuthNotifier extends StateNotifier<MerchantAuthState> {
  final MerchantRepository _repository;

  MerchantAuthNotifier(this._repository) : super(const MerchantAuthState());

  Future<void> login({required String email, required String password}) async {
    state = state.copyWith(isLoading: true, error: null);
    final result = await _repository.login(email: email, password: password);
    if (result.isSuccess && result.data != null) {
      state = MerchantAuthState(
        merchant: result.data,
        isAuthenticated: true,
      );
    } else {
      state = state.copyWith(isLoading: false, error: result.error);
    }
  }

  void logout() {
    state = const MerchantAuthState();
  }

  void clearError() {
    state = state.copyWith(error: null);
  }
}

final merchantDashboardProvider = FutureProvider<Merchant>((ref) async {
  final result = await ref.read(merchantRepositoryProvider).getDashboard();
  if (result.isSuccess && result.data != null) return result.data!;
  throw result.error ?? 'Failed to load dashboard';
});

final merchantOffersProvider = FutureProvider<List<Offer>>((ref) async {
  final result = await ref.read(merchantRepositoryProvider).getOffers();
  if (result.isSuccess) return result.data ?? [];
  throw result.error ?? 'Failed to load offers';
});

final merchantOrdersProvider = FutureProvider<List<Order>>((ref) async {
  final result = await ref.read(merchantRepositoryProvider).getOrders();
  if (result.isSuccess) return result.data ?? [];
  throw result.error ?? 'Failed to load orders';
});

final merchantReportsProvider = FutureProvider<Map<String, dynamic>>((ref) async {
  final result = await ref.read(merchantRepositoryProvider).getReports();
  if (result.isSuccess) return result.data ?? {};
  throw result.error ?? 'Failed to load reports';
});
