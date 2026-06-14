class ApiResponse<T> {
  final bool success;
  final String message;
  final T? data;
  final Map<String, dynamic>? meta;
  final Map<String, dynamic>? errors;

  ApiResponse({
    required this.success,
    required this.message,
    this.data,
    this.meta,
    this.errors,
  });

  factory ApiResponse.fromJson(
    Map<String, dynamic> json, {
    T Function(dynamic)? fromJsonT,
  }) {
    return ApiResponse(
      success: json['success'] as bool? ?? json['status'] == 'success',
      message: json['message'] as String? ?? '',
      data: json['data'] != null && fromJsonT != null
          ? fromJsonT(json['data'])
          : json['data'] as T?,
      meta: json['meta'] as Map<String, dynamic>?,
      errors: json['errors'] as Map<String, dynamic>?,
    );
  }

  Map<String, dynamic> toJson({
    Map<String, dynamic> Function(T?)? toJsonT,
  }) {
    return {
      'success': success,
      'message': message,
      'data': toJsonT != null ? toJsonT(data) : data,
      if (meta != null) 'meta': meta,
      if (errors != null) 'errors': errors,
    };
  }
}

class PaginatedResponse<T> {
  final List<T> items;
  final int total;
  final int page;
  final int pageSize;
  final int totalPages;
  final bool hasMore;

  PaginatedResponse({
    required this.items,
    required this.total,
    required this.page,
    required this.pageSize,
    required this.totalPages,
    required this.hasMore,
  });

  factory PaginatedResponse.fromJson(
    Map<String, dynamic> json, {
    required T Function(dynamic) fromJsonT,
  }) {
    final data = json['data'] as List<dynamic>? ?? [];
    final meta = json['meta'] as Map<String, dynamic>? ?? {};
    final pagination = meta['pagination'] as Map<String, dynamic>? ?? {};

    return PaginatedResponse(
      items: data.map((item) => fromJsonT(item)).toList(),
      total: pagination['total'] as int? ?? 0,
      page: pagination['page'] as int? ?? 1,
      pageSize: pagination['page_size'] as int? ?? 20,
      totalPages: pagination['total_pages'] as int? ?? 1,
      hasMore: pagination['has_more'] as bool? ?? false,
    );
  }
}
