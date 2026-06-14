class RouteNames {
  RouteNames._();

  // Auth Routes
  static const String splash = '/';
  static const String onboarding = '/onboarding';
  static const String login = '/login';
  static const String register = '/register';
  static const String forgotPassword = '/forgot-password';
  static const String verifyOtp = '/verify-otp';
  static const String resetPassword = '/reset-password';
  static const String socialLogin = '/social-login';

  // Main Routes
  static const String main = '/main';
  static const String home = '/main/home';
  static const String map = '/main/map';
  static const String search = '/search';
  static const String category = '/category';

  // Offer Routes
  static const String offerDetails = '/offer';
  static const String offerList = '/offers';

  // Store Routes
  static const String storeDetails = '/store';
  static const String storeList = '/stores';

  // Cart Routes
  static const String cart = '/cart';

  // Checkout Routes
  static const String checkout = '/checkout';
  static const String orderConfirmation = '/order-confirmation';

  // Order Routes
  static const String orders = '/orders';
  static const String orderDetails = '/order';
  static const String orderTracking = '/order-tracking';

  // Profile Routes
  static const String profile = '/profile';
  static const String editProfile = '/profile/edit';
  static const String savedAddresses = '/profile/addresses';
  static const String addAddress = '/profile/addresses/add';
  static const String editAddress = '/profile/addresses/edit';
  static const String favoriteStores = '/profile/favorites';
  static const String paymentMethods = '/profile/payment-methods';
  static const String addPaymentMethod = '/profile/payment-methods/add';
  static const String notifications = '/profile/notifications';
  static const String settings = '/profile/settings';

  // Merchant Routes
  static const String merchantLogin = '/merchant/login';
  static const String merchantRegister = '/merchant/register';
  static const String merchantDashboard = '/merchant/dashboard';
  static const String merchantOffers = '/merchant/offers';
  static const String merchantCreateOffer = '/merchant/offers/create';
  static const String merchantEditOffer = '/merchant/offers/edit';
  static const String merchantOrders = '/merchant/orders';
  static const String merchantOrderDetails = '/merchant/order';
  static const String merchantReports = '/merchant/reports';
  static const String merchantSalesAnalytics = '/merchant/analytics/sales';
  static const String merchantBestSelling = '/merchant/analytics/best-selling';
  static const String merchantRevenue = '/merchant/analytics/revenue';
  static const String merchantProfile = '/merchant/profile';
  static const String merchantSettings = '/merchant/settings';
}
