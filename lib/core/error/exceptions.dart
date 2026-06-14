class ServerException implements Exception {
  final String message;
  final int? statusCode;
  final dynamic data;

  ServerException({
    required this.message,
    this.statusCode,
    this.data,
  });

  @override
  String toString() => 'ServerException: $message (statusCode: $statusCode)';
}

class CacheException implements Exception {
  final String message;

  CacheException({required this.message});

  @override
  String toString() => 'CacheException: $message';
}

class NetworkException implements Exception {
  final String message;

  NetworkException({required this.message});

  @override
  String toString() => 'NetworkException: $message';
}

class AuthException implements Exception {
  final String message;
  final int? statusCode;

  AuthException({required this.message, this.statusCode});

  @override
  String toString() => 'AuthException: $message';
}

class ValidationException implements Exception {
  final String message;
  final Map<String, dynamic>? errors;

  ValidationException({required this.message, this.errors});

  @override
  String toString() => 'ValidationException: $message';
}

class PaymentException implements Exception {
  final String message;
  final String? paymentIntentId;

  PaymentException({required this.message, this.paymentIntentId});

  @override
  String toString() => 'PaymentException: $message';
}

class PermissionException implements Exception {
  final String message;

  PermissionException({required this.message});

  @override
  String toString() => 'PermissionException: $message';
}

class NotFoundException implements Exception {
  final String message;

  NotFoundException({required this.message});

  @override
  String toString() => 'NotFoundException: $message';
}

class TimeoutException implements Exception {
  final String message;

  TimeoutException({required this.message});

  @override
  String toString() => 'TimeoutException: $message';
}

class UnknownException implements Exception {
  final String message;

  UnknownException({required this.message});

  @override
  String toString() => 'UnknownException: $message';
}
