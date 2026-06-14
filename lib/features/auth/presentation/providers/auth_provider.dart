import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/datasources/auth_remote_datasource.dart';
import '../../data/repositories/auth_repository_impl.dart';
import '../../domain/entities/user.dart';
import '../../domain/repositories/auth_repository.dart';
import '../../../../core/network/dio_client.dart';
import '../../../../core/storage/secure_storage.dart';

final secureStorageProvider = Provider<SecureStorage>((ref) {
  return SecureStorage();
});

final dioClientProvider = Provider<DioClient>((ref) {
  return DioClient();
});

final authRemoteDataSourceProvider = Provider<AuthRemoteDataSource>((ref) {
  return AuthRemoteDataSource(
    dioClient: ref.read(dioClientProvider),
    secureStorage: ref.read(secureStorageProvider),
  );
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepositoryImpl(
    remoteDataSource: ref.read(authRemoteDataSourceProvider),
  );
});

enum AuthStatus { initial, authenticated, unauthenticated, loading }

class AuthState {
  final AuthStatus status;
  final User? user;
  final String? error;
  final bool isLoading;

  const AuthState({
    this.status = AuthStatus.initial,
    this.user,
    this.error,
    this.isLoading = false,
  });

  AuthState copyWith({
    AuthStatus? status,
    User? user,
    String? error,
    bool? isLoading,
  }) {
    return AuthState(
      status: status ?? this.status,
      user: user ?? this.user,
      error: error,
      isLoading: isLoading ?? this.isLoading,
    );
  }

  bool get isAuthenticated => status == AuthStatus.authenticated;
}

class AuthNotifier extends StateNotifier<AuthState> {
  final AuthRepository _authRepository;

  AuthNotifier(this._authRepository) : super(const AuthState()) {
    _checkAuthStatus();
  }

  Future<void> _checkAuthStatus() async {
    final result = await _authRepository.isLoggedIn();
    if (result.isSuccess && result.data == true) {
      final userResult = await _authRepository.getCurrentUser();
      if (userResult.isSuccess && userResult.data != null) {
        state = AuthState(
          status: AuthStatus.authenticated,
          user: userResult.data,
        );
        return;
      }
    }
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  Future<void> login({
    required String email,
    required String password,
  }) async {
    state = state.copyWith(isLoading: true, error: null);
    final result = await _authRepository.login(
      email: email,
      password: password,
    );
    if (result.isSuccess && result.data != null) {
      state = AuthState(
        status: AuthStatus.authenticated,
        user: result.data,
      );
    } else {
      state = state.copyWith(
        isLoading: false,
        error: result.error,
        status: AuthStatus.unauthenticated,
      );
    }
  }

  Future<void> register({
    required String name,
    required String email,
    required String password,
    String? phone,
  }) async {
    state = state.copyWith(isLoading: true, error: null);
    final result = await _authRepository.register(
      name: name,
      email: email,
      password: password,
      phone: phone,
    );
    if (result.isSuccess && result.data != null) {
      state = AuthState(
        status: AuthStatus.authenticated,
        user: result.data,
      );
    } else {
      state = state.copyWith(
        isLoading: false,
        error: result.error,
        status: AuthStatus.unauthenticated,
      );
    }
  }

  Future<String?> forgotPassword(String email) async {
    state = state.copyWith(isLoading: true, error: null);
    final result = await _authRepository.forgotPassword(email);
    state = state.copyWith(isLoading: false, error: result.error);
    return result.error;
  }

  Future<String?> verifyOtp({
    required String email,
    required String otp,
  }) async {
    state = state.copyWith(isLoading: true, error: null);
    final result = await _authRepository.verifyOtp(email: email, otp: otp);
    state = state.copyWith(isLoading: false, error: result.error);
    return result.error;
  }

  Future<String?> resetPassword({
    required String email,
    required String otp,
    required String newPassword,
  }) async {
    state = state.copyWith(isLoading: true, error: null);
    final result = await _authRepository.resetPassword(
      email: email,
      otp: otp,
      newPassword: newPassword,
    );
    state = state.copyWith(isLoading: false, error: result.error);
    return result.error;
  }

  Future<void> socialLogin({
    required String provider,
    required String token,
  }) async {
    state = state.copyWith(isLoading: true, error: null);
    final result = await _authRepository.socialLogin(
      provider: provider,
      token: token,
    );
    if (result.isSuccess && result.data != null) {
      state = AuthState(
        status: AuthStatus.authenticated,
        user: result.data,
      );
    } else {
      state = state.copyWith(
        isLoading: false,
        error: result.error,
      );
    }
  }

  Future<void> logout() async {
    state = state.copyWith(isLoading: true);
    await _authRepository.logout();
    state = const AuthState(status: AuthStatus.unauthenticated);
  }

  void clearError() {
    state = state.copyWith(error: null);
  }
}

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  final repository = ref.read(authRepositoryProvider);
  return AuthNotifier(repository);
});
