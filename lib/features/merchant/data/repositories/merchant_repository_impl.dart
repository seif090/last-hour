import 'dart:io';
import 'package:dio/dio.dart';
import '../../../../core/utils/result.dart';
import '../../../offers/domain/entities/offer.dart';
import '../../../orders/domain/entities/order.dart';
import '../../domain/entities/merchant.dart';
import '../../domain/repositories/merchant_repository.dart';
import '../datasources/merchant_remote_data_source.dart';

class MerchantRepositoryImpl implements MerchantRepository {
  final MerchantRemoteDataSource _remoteDataSource;

  MerchantRepositoryImpl(this._remoteDataSource);

  @override
  Future<Result<Merchant>> login({required String email, required String password}) async {
    try {
      final response = await _remoteDataSource.login(email: email, password: password);
      final data = response.data;
      if (data == null) return Result.failure('Invalid response');
      return Result.success(data.toEntity());
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<Merchant>> getDashboard() async {
    try {
      final response = await _remoteDataSource.getDashboard();
      final data = response.data;
      if (data == null) return Result.failure('Invalid response');
      return Result.success(data.toEntity());
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<List<Offer>>> getOffers() async {
    try {
      final response = await _remoteDataSource.getOffers();
      return Result.success(
        response.items.map((model) => model.toEntity()).toList(),
      );
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
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
  }) async {
    try {
      final response = await _remoteDataSource.createOffer(
        title: title,
        description: description,
        category: category,
        originalPrice: originalPrice,
        discountPrice: discountPrice,
        quantity: quantity,
        image: imagePath != null ? File(imagePath) : null,
        pickupStart: pickupStart?.toIso8601String(),
        pickupEnd: pickupEnd?.toIso8601String(),
      );
      final data = response.data;
      if (data == null) return Result.failure('Invalid response');
      return Result.success(data.toEntity());
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<Offer>> updateOffer({
    required String offerId,
    String? title,
    String? description,
    double? originalPrice,
    double? discountPrice,
    int? quantity,
  }) async {
    try {
      final response = await _remoteDataSource.updateOffer(
        offerId: offerId,
        title: title,
        description: description,
        originalPrice: originalPrice,
        discountPrice: discountPrice,
        quantity: quantity,
      );
      final data = response.data;
      if (data == null) return Result.failure('Invalid response');
      return Result.success(data.toEntity());
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<void>> toggleOfferActive(String offerId, bool isActive) async {
    try {
      await _remoteDataSource.toggleOfferActive(offerId, isActive);
      return Result.success(null);
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<List<Order>>> getOrders({String? status}) async {
    try {
      final response = await _remoteDataSource.getOrders(status: status);
      return Result.success(
        response.items.map((model) => model.toEntity()).toList(),
      );
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<Order>> updateOrderStatus({
    required String orderId,
    required String status,
  }) async {
    try {
      final response = await _remoteDataSource.updateOrderStatus(
        orderId: orderId,
        status: status,
      );
      final data = response.data;
      if (data == null) return Result.failure('Invalid response');
      return Result.success(data.toEntity());
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  @override
  Future<Result<Map<String, dynamic>>> getReports({String? period}) async {
    try {
      final data = await _remoteDataSource.getReports(period: period);
      return Result.success(data);
    } on DioException catch (e) {
      return Result.failure(_mapError(e));
    }
  }

  String _mapError(DioException e) {
    if (e.response?.data is Map) {
      return (e.response!.data as Map)['message'] as String? ?? e.message ?? 'Something went wrong';
    }
    return e.message ?? 'Something went wrong';
  }
}
