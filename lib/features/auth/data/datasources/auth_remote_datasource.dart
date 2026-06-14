import '../../../../core/constants/api_constants.dart';
import '../../../../core/network/dio_client.dart';
import '../../../../core/storage/secure_storage.dart';
import '../models/user_model.dart';

class AuthRemoteDataSource {
  final DioClient _dioClient;
  final SecureStorage _secureStorage;

  AuthRemoteDataSource({
    required DioClient dioClient,
    required SecureStorage secureStorage,
  })  : _dioClient = dioClient,
        _secureStorage = secureStorage;

  Future<UserModel> login({
    required String email,
    required String password,
  }) async {
    final response = await _dioClient.post(
      ApiConstants.login,
      data: {
        'email': email,
        'password': password,
      },
    );
    final data = response.data['data'] as Map<String, dynamic>;
    final user = UserModel.fromJson(data);
    if (user.accessToken != null) {
      await _secureStorage.saveToken(user.accessToken!);
    }
    if (user.refreshToken != null) {
      await _secureStorage.saveRefreshToken(user.refreshToken!);
    }
    await _secureStorage.saveUserData(response.data.toString());
    return user;
  }

  Future<UserModel> register({
    required String name,
    required String email,
    required String password,
    String? phone,
  }) async {
    final response = await _dioClient.post(
      ApiConstants.register,
      data: {
        'name': name,
        'email': email,
        'password': password,
        if (phone != null) 'phone': phone,
      },
    );
    final data = response.data['data'] as Map<String, dynamic>;
    final user = UserModel.fromJson(data);
    if (user.accessToken != null) {
      await _secureStorage.saveToken(user.accessToken!);
    }
    if (user.refreshToken != null) {
      await _secureStorage.saveRefreshToken(user.refreshToken!);
    }
    await _secureStorage.saveUserData(response.data.toString());
    return user;
  }

  Future<void> forgotPassword(String email) async {
    await _dioClient.post(
      ApiConstants.forgotPassword,
      data: {'email': email},
    );
  }

  Future<void> verifyOtp({
    required String email,
    required String otp,
  }) async {
    await _dioClient.post(
      ApiConstants.verifyOtp,
      data: {
        'email': email,
        'otp': otp,
      },
    );
  }

  Future<void> resetPassword({
    required String email,
    required String otp,
    required String newPassword,
  }) async {
    await _dioClient.post(
      ApiConstants.resetPassword,
      data: {
        'email': email,
        'otp': otp,
        'password': newPassword,
        'password_confirmation': newPassword,
      },
    );
  }

  Future<UserModel> socialLogin({
    required String provider,
    required String token,
  }) async {
    final response = await _dioClient.post(
      ApiConstants.socialLogin,
      data: {
        'provider': provider,
        'access_token': token,
      },
    );
    final data = response.data['data'] as Map<String, dynamic>;
    final user = UserModel.fromJson(data);
    if (user.accessToken != null) {
      await _secureStorage.saveToken(user.accessToken!);
    }
    if (user.refreshToken != null) {
      await _secureStorage.saveRefreshToken(user.refreshToken!);
    }
    await _secureStorage.saveUserData(response.data.toString());
    return user;
  }

  Future<void> logout() async {
    try {
      await _dioClient.post(ApiConstants.login);
    } catch (_) {}
    await _secureStorage.clearAuth();
  }

  Future<UserModel?> getCurrentUser() async {
    try {
      final response = await _dioClient.get(ApiConstants.userProfile);
      final data = response.data['data'] as Map<String, dynamic>;
      return UserModel.fromJson(data);
    } catch (_) {
      final cached = await _secureStorage.getUserData();
      if (cached != null) {
        try {
          return UserModel.fromJson(
            Map<String, dynamic>.from({'id': '', 'name': '', 'email': '', 'created_at': ''}),
          );
        } catch (_) {
          return null;
        }
      }
      return null;
    }
  }

  Future<bool> hasToken() async {
    final token = await _secureStorage.getToken();
    return token != null && token.isNotEmpty;
  }
}
