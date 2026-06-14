import '../entities/user.dart';
import '../../../../core/utils/result.dart';

abstract class AuthRepository {
  Future<Result<User>> login({
    required String email,
    required String password,
  });

  Future<Result<User>> register({
    required String name,
    required String email,
    required String password,
    String? phone,
  });

  Future<Result<void>> forgotPassword(String email);

  Future<Result<void>> verifyOtp({
    required String email,
    required String otp,
  });

  Future<Result<void>> resetPassword({
    required String email,
    required String otp,
    required String newPassword,
  });

  Future<Result<User>> socialLogin({
    required String provider,
    required String token,
  });

  Future<Result<void>> logout();

  Future<Result<User>> getCurrentUser();

  Future<Result<bool>> isLoggedIn();
}
