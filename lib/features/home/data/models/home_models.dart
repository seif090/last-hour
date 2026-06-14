import 'package:flutter/material.dart';

class Category {
  final String id;
  final String name;
  final IconData icon;
  final String imageUrl;
  final Color color;

  const Category({
    required this.id,
    required this.name,
    required this.icon,
    required this.imageUrl,
    required this.color,
  });

  static List<Category> get mockCategories => [
        Category(
          id: '1',
          name: 'Restaurants',
          icon: Icons.restaurant_rounded,
          imageUrl: '',
          color: const Color(0xFFE53935),
        ),
        Category(
          id: '2',
          name: 'Bakeries',
          icon: Icons.bakery_dining_rounded,
          imageUrl: '',
          color: const Color(0xFFFF9800),
        ),
        Category(
          id: '3',
          name: 'Desserts',
          icon: Icons.icecream_rounded,
          imageUrl: '',
          color: const Color(0xFFE91E63),
        ),
        Category(
          id: '4',
          name: 'Supermarkets',
          icon: Icons.local_grocery_store_rounded,
          imageUrl: '',
          color: const Color(0xFF4CAF50),
        ),
      ];
}

class HomeOffer {
  final String id;
  final String title;
  final String storeName;
  final String storeId;
  final String imageUrl;
  final double originalPrice;
  final double discountPrice;
  final int remainingQuantity;
  final DateTime expiryTime;
  final double distance;
  final double rating;
  final String category;

  const HomeOffer({
    required this.id,
    required this.title,
    required this.storeName,
    required this.storeId,
    required this.imageUrl,
    required this.originalPrice,
    required this.discountPrice,
    required this.remainingQuantity,
    required this.expiryTime,
    required this.distance,
    required this.rating,
    required this.category,
  });

  static List<HomeOffer> get mockOffers => [
        HomeOffer(
          id: '1',
          title: 'Mixed Sushi Box',
          storeName: 'Sakura Japanese',
          storeId: 's1',
          imageUrl: '',
          originalPrice: 24.99,
          discountPrice: 9.99,
          remainingQuantity: 3,
          expiryTime: DateTime.now().add(const Duration(hours: 2, minutes: 30)),
          distance: 1.2,
          rating: 4.8,
          category: 'Restaurants',
        ),
        HomeOffer(
          id: '2',
          title: 'Artisan Croissants (6pk)',
          storeName: 'Le Pain Bakery',
          storeId: 's2',
          imageUrl: '',
          originalPrice: 18.00,
          discountPrice: 6.50,
          remainingQuantity: 5,
          expiryTime: DateTime.now().add(const Duration(hours: 1, minutes: 15)),
          distance: 0.8,
          rating: 4.6,
          category: 'Bakeries',
        ),
        HomeOffer(
          id: '3',
          title: 'Chocolate Cake Slice',
          storeName: 'Sweet Dreams',
          storeId: 's3',
          imageUrl: '',
          originalPrice: 8.50,
          discountPrice: 3.99,
          remainingQuantity: 2,
          expiryTime: DateTime.now().add(const Duration(hours: 3, minutes: 45)),
          distance: 2.1,
          rating: 4.9,
          category: 'Desserts',
        ),
        HomeOffer(
          id: '4',
          title: 'Fresh Salad Bowl',
          storeName: 'Fresh Mart',
          storeId: 's4',
          imageUrl: '',
          originalPrice: 12.00,
          discountPrice: 4.99,
          remainingQuantity: 8,
          expiryTime: DateTime.now().add(const Duration(hours: 4, minutes: 0)),
          distance: 0.5,
          rating: 4.3,
          category: 'Supermarkets',
        ),
        HomeOffer(
          id: '5',
          title: 'Margherita Pizza',
          storeName: 'Pizza Roma',
          storeId: 's5',
          imageUrl: '',
          originalPrice: 15.99,
          discountPrice: 5.99,
          remainingQuantity: 4,
          expiryTime: DateTime.now().add(const Duration(hours: 2, minutes: 0)),
          distance: 1.5,
          rating: 4.7,
          category: 'Restaurants',
        ),
        HomeOffer(
          id: '6',
          title: 'Organic Fruit Box',
          storeName: 'Green Grocers',
          storeId: 's6',
          imageUrl: '',
          originalPrice: 20.00,
          discountPrice: 7.99,
          remainingQuantity: 6,
          expiryTime: DateTime.now().add(const Duration(hours: 5, minutes: 30)),
          distance: 0.3,
          rating: 4.4,
          category: 'Supermarkets',
        ),
      ];
}
