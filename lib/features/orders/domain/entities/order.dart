class OrderItem {
  final String id;
  final String offerId;
  final String offerTitle;
  final String? offerImageUrl;
  final double price;
  final int quantity;

  const OrderItem({
    required this.id,
    required this.offerId,
    required this.offerTitle,
    this.offerImageUrl,
    required this.price,
    required this.quantity,
  });

  double get total => price * quantity;
}

enum OrderStatus {
  pending,
  confirmed,
  preparing,
  ready,
  pickedUp,
  delivered,
  cancelled,
  rejected;

  String get displayName {
    switch (this) {
      case OrderStatus.pending:
        return 'Pending';
      case OrderStatus.confirmed:
        return 'Confirmed';
      case OrderStatus.preparing:
        return 'Preparing';
      case OrderStatus.ready:
        return 'Ready for Pickup';
      case OrderStatus.pickedUp:
        return 'Picked Up';
      case OrderStatus.delivered:
        return 'Delivered';
      case OrderStatus.cancelled:
        return 'Cancelled';
      case OrderStatus.rejected:
        return 'Rejected';
    }
  }
}

class Order {
  final String id;
  final String storeId;
  final String storeName;
  final String? storeLogoUrl;
  final List<OrderItem> items;
  final double subtotal;
  final double serviceFee;
  final double deliveryFee;
  final double discount;
  final double total;
  final OrderStatus status;
  final String? couponCode;
  final String paymentMethod;
  final bool isDelivery;
  final String? deliveryAddress;
  final DateTime createdAt;
  final DateTime? estimatedPickupTime;
  final DateTime? pickedUpAt;
  final String? notes;

  const Order({
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
}
