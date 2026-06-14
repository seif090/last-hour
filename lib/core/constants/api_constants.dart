class ApiConstants {
  ApiConstants._();

  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://api.lasthour.app/v1',
  );

  static const String secretKey = String.fromEnvironment(
    'API_SECRET_KEY',
  );

  // Timeouts
  static const Duration connectTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);
  static const Duration sendTimeout = Duration(seconds: 30);

  // Auth endpoints
  static const String login = '/auth/login';
  static const String register = '/auth/register';
  static const String forgotPassword = '/auth/forgot-password';
  static const String verifyOtp = '/auth/verify-otp';
  static const String resetPassword = '/auth/reset-password';
  static const String refreshToken = '/auth/refresh-token';
  static const String socialLogin = '/auth/social-login';

  // User endpoints
  static const String userProfile = '/user/profile';
  static const String updateProfile = '/user/profile';
  static const String savedAddresses = '/user/addresses';
  static const String addAddress = '/user/addresses';
  static const String favoriteStores = '/user/favorites';
  static const String toggleFavorite = '/user/favorites';
  static const String paymentMethods = '/user/payment-methods';
  static const String notificationSettings = '/user/notifications';

  // Store endpoints
  static const String stores = '/stores';
  static const String storeDetails = '/stores/';
  static const String storeProducts = '/stores/';
  static const String nearbyStores = '/stores/nearby';

  // Offer endpoints
  static const String offers = '/offers';
  static const String offerDetails = '/offers/';
  static const String featuredOffers = '/offers/featured';
  static const String offersByCategory = '/offers/category/';
  static const String offersByStore = '/offers/store/';

  // Category endpoints
  static const String categories = '/categories';

  // Cart endpoints
  static const String cart = '/cart';
  static const String addToCart = '/cart/add';
  static const String updateCartItem = '/cart/update';
  static const String removeFromCart = '/cart/remove';
  static const String applyCoupon = '/cart/coupon';
  static const String removeCoupon = '/cart/coupon';

  // Order endpoints
  static const String orders = '/orders';
  static const String orderDetails = '/orders/';
  static const String createOrder = '/orders';
  static const String cancelOrder = '/orders/';
  static const String trackOrder = '/orders/';

  // Payment endpoints
  static const String createPaymentIntent = '/payments/create-intent';
  static const String confirmPayment = '/payments/confirm';
  static const String paymentMethodsList = '/payments/methods';

  // Merchant endpoints
  static const String merchantLogin = '/merchant/auth/login';
  static const String merchantRegister = '/merchant/auth/register';
  static const String merchantDashboard = '/merchant/dashboard';
  static const String merchantOffers = '/merchant/offers';
  static const String merchantCreateOffer = '/merchant/offers';
  static const String merchantOrders = '/merchant/orders';
  static const String merchantOrderUpdate = '/merchant/orders/';
  static const String merchantReports = '/merchant/reports';
  static const String merchantSalesAnalytics = '/merchant/analytics/sales';
  static const String merchantBestSelling = '/merchant/analytics/best-selling';
  static const String merchantRevenue = '/merchant/analytics/revenue';

  // Notification endpoints
  static const String notifications = '/notifications';
  static const String markNotificationRead = '/notifications/';
  static const String registerDeviceToken = '/notifications/device';
}
