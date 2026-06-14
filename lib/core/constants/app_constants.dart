class AppConstants {
  AppConstants._();

  static const String appName = 'Last Hour';
  static const String appVersion = '1.0.0';
  static const String packageName = 'com.lasthour.last_hour';

  // Pagination
  static const int defaultPageSize = 20;
  static const int defaultPage = 1;

  // Location
  static const double defaultLatitude = 30.0444;
  static const double defaultLongitude = 31.2357;
  static const double nearbyRadiusKm = 10.0;

  // Countdown
  static const int maxOfferExpiryHours = 24;

  // Debounce
  static const Duration searchDebounce = Duration(milliseconds: 500);
  static const Duration locationDebounce = Duration(milliseconds: 300);

  // Cache
  static const String onboardingKey = 'onboarding_completed';
  static const String authTokenKey = 'auth_token';
  static const String refreshTokenKey = 'refresh_token';
  static const String userDataKey = 'user_data';
  static const String themeModeKey = 'theme_mode';
  static const String localeKey = 'locale';
  static const String cartCacheKey = 'cart_cache';

  // Date formats
  static const String dateFormat = 'yyyy-MM-dd';
  static const String timeFormat = 'HH:mm';
  static const String dateTimeFormat = 'yyyy-MM-dd HH:mm:ss';
  static const String displayDateFormat = 'MMM dd, yyyy';
  static const String displayTimeFormat = 'hh:mm a';
  static const String displayDateTimeFormat = 'MMM dd, yyyy hh:mm a';

  // Currency
  static const String currencySymbol = '\$';
  static const String currencyCode = 'USD';

  // Distance
  static const String distanceUnit = 'km';

  // Order status
  static const String orderPending = 'pending';
  static const String orderConfirmed = 'confirmed';
  static const String orderPreparing = 'preparing';
  static const String orderReady = 'ready';
  static const String orderPickedUp = 'picked_up';
  static const String orderDelivered = 'delivered';
  static const String orderCancelled = 'cancelled';
  static const String orderRejected = 'rejected';

  // Payment methods
  static const String paymentCash = 'cash';
  static const String paymentCard = 'card';
  static const String paymentWallet = 'wallet';

  // Offer status
  static const String offerActive = 'active';
  static const String offerExpired = 'expired';
  static const String offerSoldOut = 'sold_out';
  static const String offerPaused = 'paused';
}
