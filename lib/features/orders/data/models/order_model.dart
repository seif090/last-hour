import '../../domain/entities/order.dart';

class OrderItemModel {
  final String id;
  final String offerId;
  final String offerTitle;
  final String? offerImageUrl;
  final double price;
  final int quantity;

  const OrderItemModel({
    required this.id,
    required this.offerId,
    required this.offerTitle,
    this.offerImageUrl,
    required this.price,
    required this.quantity,
  });

  factory OrderItemModel.fromJson(Map<String, dynamic> json) {
    return OrderItemModel(
      id: json['id']?.toString() ?? '',
      offerId: json['offer_id']?.toString() ?? '',
      offerTitle: json['offer_title'] as String? ?? '',
      offerImageUrl: json['offer_image_url'] as String?,
      price: (json['price'] as num?)?.toDouble() ?? 0,
      quantity: json['quantity'] as int? ?? 1,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'offer_id': offerId,
    'offer_title': offerTitle,
    'offer_image_url': offerImageUrl,
    'price': price,
    'quantity': quantity,
  };

  OrderItem toEntity() => OrderItem(
    id: id, offerId: offerId, offerTitle: offerTitle,
    offerImageUrl: offerImageUrl, price: price, quantity: quantity,
  );
}

class OrderModel {
  final String id;
  final String storeId;
  final String storeName;
  final String? storeLogoUrl;
  final List<OrderItemModel> items;
  final double subtotal;
  final double serviceFee;
  final double deliveryFee;
  final double discount;
  final double total;
  final String status;
  final String? couponCode;
  final String paymentMethod;
  final bool isDelivery;
  final String? deliveryAddress;
  final String createdAt;
  final String? estimatedPickupTime;
  final String? pickedUpAt;
  final String? notes;

  const OrderModel({
    required this.id,
    required this.storeId,
    required this.storeName,
    this.storeLogoUrl,
    required this.items,
    required this.subtotal,
    this.serviceFee = 0,
    this.deliveryFee = 0,
    this.discount = 0,
    required this.total,
    required this.status,
    this.couponCode,
    required this.paymentMethod,
    this.isDelivery = false,
    this.deliveryAddress,
    required this.createdAt,
    this.estimatedPickupTime,
    this.pickedUpAt,
    this.notes,
  });

  factory OrderModel.fromJson(Map<String, dynamic> json) {
    return OrderModel(
      id: json['id']?.toString() ?? '',
      storeId: json['store_id']?.toString() ?? '',
      storeName: json['store_name'] as String? ?? '',
      storeLogoUrl: json['store_logo_url'] as String?,
      items: (json['items'] as List<dynamic>?)
          ?.map((e) => OrderItemModel.fromJson(e as Map<String, dynamic>))
          .toList() ?? [],
      subtotal: (json['subtotal'] as num?)?.toDouble() ?? 0,
      serviceFee: (json['service_fee'] as num?)?.toDouble() ?? 0,
      deliveryFee: (json['delivery_fee'] as num?)?.toDouble() ?? 0,
      discount: (json['discount'] as num?)?.toDouble() ?? 0,
      total: (json['total'] as num?)?.toDouble() ?? 0,
      status: json['status'] as String? ?? 'pending',
      couponCode: json['coupon_code'] as String?,
      paymentMethod: json['payment_method'] as String? ?? 'cash',
      isDelivery: json['is_delivery'] as bool? ?? false,
      deliveryAddress: json['delivery_address'] as String?,
      createdAt: json['created_at'] as String? ?? '',
      estimatedPickupTime: json['estimated_pickup_time'] as String?,
      pickedUpAt: json['picked_up_at'] as String?,
      notes: json['notes'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'store_id': storeId,
    'store_name': storeName,
    'store_logo_url': storeLogoUrl,
    'items': items.map((e) => e.toJson()).toList(),
    'subtotal': subtotal,
    'service_fee': serviceFee,
    'delivery_fee': deliveryFee,
    'discount': discount,
    'total': total,
    'status': status,
    'coupon_code': couponCode,
    'payment_method': paymentMethod,
    'is_delivery': isDelivery,
    'delivery_address': deliveryAddress,
    'created_at': createdAt,
    'estimated_pickup_time': estimatedPickupTime,
    'picked_up_at': pickedUpAt,
    'notes': notes,
  };

  Order toEntity() {
    OrderStatus orderStatus;
    switch (status) {
      case 'confirmed': orderStatus = OrderStatus.confirmed;
      case 'preparing': orderStatus = OrderStatus.preparing;
      case 'ready': orderStatus = OrderStatus.ready;
      case 'picked_up': orderStatus = OrderStatus.pickedUp;
      case 'delivered': orderStatus = OrderStatus.delivered;
      case 'cancelled': orderStatus = OrderStatus.cancelled;
      case 'rejected': orderStatus = OrderStatus.rejected;
      default: orderStatus = OrderStatus.pending;
    }
    return Order(
      id: id, storeId: storeId, storeName: storeName,
      storeLogoUrl: storeLogoUrl,
      items: items.map((e) => e.toEntity()).toList(),
      subtotal: subtotal, serviceFee: serviceFee,
      deliveryFee: deliveryFee, discount: discount,
      total: total, status: orderStatus,
      couponCode: couponCode, paymentMethod: paymentMethod,
      isDelivery: isDelivery, deliveryAddress: deliveryAddress,
      createdAt: DateTime.tryParse(createdAt) ?? DateTime.now(),
      estimatedPickupTime: estimatedPickupTime != null ? DateTime.tryParse(estimatedPickupTime!) : null,
      pickedUpAt: pickedUpAt != null ? DateTime.tryParse(pickedUpAt!) : null,
      notes: notes,
    );
  }
}
