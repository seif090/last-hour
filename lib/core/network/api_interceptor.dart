import 'package:dio/dio.dart';
import '../constants/api_constants.dart';
import '../storage/secure_storage.dart';

class ApiInterceptor extends Interceptor {
  final SecureStorage secureStorage;

  ApiInterceptor({required this.secureStorage});

  @override
  void onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final token = await secureStorage.getToken();
    if (token != null && token.isNotEmpty) {
      options.headers['Authorization'] = 'Bearer $token';
    }
    handler.next(options);
  }

  @override
  void onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    if (err.response?.statusCode == 401) {
      final refreshed = await _refreshToken();
      if (refreshed) {
        final token = await secureStorage.getToken();
        if (token != null) {
          err.requestOptions.headers['Authorization'] = 'Bearer $token';
          try {
            final response = await Dio().fetch(err.requestOptions);
            handler.resolve(response);
            return;
          } catch (e) {
            handler.next(err);
            return;
          }
        }
      }
    }
    handler.next(err);
  }

  Future<bool> _refreshToken() async {
    try {
      final refreshToken = await secureStorage.getRefreshToken();
      if (refreshToken == null) return false;

      final dio = Dio(BaseOptions(baseUrl: ApiConstants.baseUrl));
      final response = await dio.post(
        ApiConstants.refreshToken,
        data: {'refresh_token': refreshToken},
      );

      if (response.statusCode == 200) {
        final newToken = response.data['data']['token'] as String;
        final newRefreshToken = response.data['data']['refresh_token'] as String;
        await secureStorage.saveToken(newToken);
        await secureStorage.saveRefreshToken(newRefreshToken);
        return true;
      }
      return false;
    } catch (e) {
      return false;
    }
  }
}
