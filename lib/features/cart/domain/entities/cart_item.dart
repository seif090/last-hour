class CartItem {
  final String id;
  final String offerId;
  final String title;
  final String? imageUrl;
  final String storeId;
  final String storeName;
  final double price;
  final int quantity;
  final int maxQuantity;
  final DateTime expiryTime;

  const CartItem({
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

  CartItem copyWith({int? quantity}) {
    return CartItem(
      id: id,
      offerId: offerId,
      title: title,
      imageUrl: imageUrl,
      storeId: storeId,
      storeName: storeName,
      price: price,
      quantity: quantity ?? this.quantity,
      maxQuantity: maxQuantity,
      expiryTime: expiryTime,
    );
  }

  double get total => price * quantity;
}
