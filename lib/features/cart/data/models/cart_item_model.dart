import '../../domain/entities/cart_item.dart';

class CartItemModel {
  final String id;
  final String offerId;
  final String title;
  final String? imageUrl;
  final String storeId;
  final String storeName;
  final double price;
  final int quantity;
  final int maxQuantity;
  final String expiryTime;

  const CartItemModel({
    required this.id,
    required this.offerId,
    required this.title,
    this.imageUrl,
    required this.storeId,
    required this.storeName,
    required this.price,
    required this.quantity,
    this.maxQuantity = 99,
    required this.expiryTime,
  });

  factory CartItemModel.fromJson(Map<String, dynamic> json) {
    return CartItemModel(
      id: json['id']?.toString() ?? '',
      offerId: json['offer_id']?.toString() ?? '',
      title: json['title'] as String? ?? '',
      imageUrl: json['image_url'] as String?,
      storeId: json['store_id']?.toString() ?? '',
      storeName: json['store_name'] as String? ?? '',
      price: (json['price'] as num?)?.toDouble() ?? 0,
      quantity: json['quantity'] as int? ?? 1,
      maxQuantity: json['max_quantity'] as int? ?? 99,
      expiryTime: json['expiry_time'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'offer_id': offerId,
    'title': title,
    'image_url': imageUrl,
    'store_id': storeId,
    'store_name': storeName,
    'price': price,
    'quantity': quantity,
    'max_quantity': maxQuantity,
    'expiry_time': expiryTime,
  };

  CartItem toEntity() => CartItem(
    id: id, offerId: offerId, title: title,
    imageUrl: imageUrl, storeId: storeId, storeName: storeName,
    price: price, quantity: quantity, maxQuantity: maxQuantity,
    expiryTime: DateTime.tryParse(expiryTime) ?? DateTime.now(),
  );

  factory CartItemModel.fromEntity(CartItem entity) => CartItemModel(
    id: entity.id, offerId: entity.offerId, title: entity.title,
    imageUrl: entity.imageUrl, storeId: entity.storeId,
    storeName: entity.storeName, price: entity.price,
    quantity: entity.quantity, maxQuantity: entity.maxQuantity,
    expiryTime: entity.expiryTime.toIso8601String(),
  );
}
