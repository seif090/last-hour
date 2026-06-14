// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appName => 'Last Hour';

  @override
  String get appTagline => 'Save food. Save money. Save the planet.';

  @override
  String get onboardingTitle1 => 'Save Surplus Food';

  @override
  String get onboardingSubtitle1 =>
      'Discover delicious meals from local restaurants, bakeries, and supermarkets at discounted prices.';

  @override
  String get onboardingTitle2 => 'Amazing Discounts';

  @override
  String get onboardingSubtitle2 =>
      'Get up to 70% off on fresh food that would otherwise go to waste. Great for your wallet and the planet!';

  @override
  String get onboardingTitle3 => 'Fight Food Waste';

  @override
  String get onboardingSubtitle3 =>
      'Join thousands of people saving food from going to waste. Every meal rescued makes a difference.';

  @override
  String get onboardingSkip => 'Skip';

  @override
  String get onboardingGetStarted => 'Get Started';

  @override
  String get onboardingNext => 'Next';

  @override
  String get welcomeBack => 'Welcome back!';

  @override
  String get signInSubtitle => 'Sign in to save food and money';

  @override
  String get email => 'Email';

  @override
  String get emailHint => 'Enter your email';

  @override
  String get password => 'Password';

  @override
  String get passwordHint => 'Enter your password';

  @override
  String get forgotPassword => 'Forgot Password?';

  @override
  String get signIn => 'Sign In';

  @override
  String get orContinueWith => 'or continue with';

  @override
  String get noAccount => 'Don\'t have an account? ';

  @override
  String get signUp => 'Sign Up';

  @override
  String get createAccount => 'Create Account';

  @override
  String get registerSubtitle => 'Join the fight against food waste';

  @override
  String get fullName => 'Full Name';

  @override
  String get fullNameHint => 'Enter your full name';

  @override
  String get phoneOptional => 'Phone (optional)';

  @override
  String get phoneHint => 'Enter your phone number';

  @override
  String get createStrongPassword => 'Create a strong password';

  @override
  String get confirmPassword => 'Confirm Password';

  @override
  String get confirmPasswordHint => 'Confirm your password';

  @override
  String get orSignUpWith => 'or sign up with';

  @override
  String get alreadyHaveAccount => 'Already have an account? ';

  @override
  String get forgotPasswordTitle => 'Forgot Password?';

  @override
  String get forgotPasswordSubtitle =>
      'Enter your email address and we\'ll send you a verification code to reset your password.';

  @override
  String get sendOtp => 'Send OTP';

  @override
  String get verifyOtpTitle => 'Verify OTP';

  @override
  String verifyOtpSubtitle(String email) {
    return 'Enter the 6-digit code sent to $email';
  }

  @override
  String get verify => 'Verify';

  @override
  String get didntReceiveCode => 'Didn\'t receive the code? ';

  @override
  String get resend => 'Resend';

  @override
  String get deliverTo => 'DELIVER TO';

  @override
  String get currentLocation => 'Current Location';

  @override
  String get orderBy9pm => 'Order by 9 PM for pickup';

  @override
  String get searchHint => 'Search offers, stores...';

  @override
  String get categories => 'Categories';

  @override
  String get featuredOffers => 'Featured Offers';

  @override
  String get seeAll => 'See All';

  @override
  String get nearbyOffers => 'Nearby Offers';

  @override
  String get noOffersInCategory => 'No offers in this category nearby';

  @override
  String get viewAllOffers => 'View All Offers';

  @override
  String get myCart => 'My Cart';

  @override
  String get clear => 'Clear';

  @override
  String get subtotal => 'Subtotal';

  @override
  String get serviceFee => 'Service Fee';

  @override
  String get total => 'Total';

  @override
  String get proceedToCheckout => 'Proceed to Checkout';

  @override
  String get cartEmptyTitle => 'Your cart is empty';

  @override
  String get cartEmptySubtitle =>
      'Looks like you haven\'t added anything yet. Browse nearby offers to find great deals!';

  @override
  String get browseOffers => 'Browse Offers';

  @override
  String get myOrders => 'My Orders';

  @override
  String get active => 'Active';

  @override
  String get completed => 'Completed';

  @override
  String get cancelled => 'Cancelled';

  @override
  String get couldNotLoadOrders => 'Could not load orders';

  @override
  String itemCount(int count) {
    return '$count item(s)';
  }

  @override
  String get noOrdersTitle => 'No orders yet';

  @override
  String get noOrdersSubtitle =>
      'You haven\'t placed any orders yet. Start exploring and save food from going to waste!';

  @override
  String get exploreOffers => 'Explore Offers';

  @override
  String get checkout => 'Checkout';

  @override
  String get delivery => 'Delivery';

  @override
  String get payment => 'Payment';

  @override
  String get review => 'Review';

  @override
  String get deliveryMode => 'Delivery Mode';

  @override
  String get pickup => 'Pickup';

  @override
  String get deliveryAddress => 'Delivery Address';

  @override
  String get home => 'Home';

  @override
  String get defaultAddress => '123 Main Street, Downtown';

  @override
  String get paymentMethod => 'Payment Method';

  @override
  String get cashOnPickup => 'Cash on Pickup';

  @override
  String get creditCard => 'Credit Card';

  @override
  String get paypal => 'PayPal';

  @override
  String get orderSummary => 'Order Summary';

  @override
  String qty(int quantity) {
    return 'Qty: $quantity';
  }

  @override
  String get deliveryFree => 'Free';

  @override
  String get continueAction => 'Continue';

  @override
  String get placeOrder => 'Place Order';

  @override
  String get couldNotLoadCart => 'Could not load cart';

  @override
  String get orderPlaced => 'Order Placed';

  @override
  String get orderPlacedSubtitle => 'Your order has been placed successfully';

  @override
  String get trackOrder => 'Track Order';

  @override
  String get backToHome => 'Back to Home';

  @override
  String orderNumber(String number) {
    return 'Order #$number';
  }

  @override
  String get store => 'Store';

  @override
  String get pickupTime => 'Pickup Time';

  @override
  String get orderTotal => 'Total';

  @override
  String get failedToLoadOffer => 'Failed to load offer';

  @override
  String get noDescription => 'No description available.';

  @override
  String remainingLeft(int count) {
    return '$count left';
  }

  @override
  String get addToCart => 'Add to Cart';

  @override
  String get addedToCart => 'Added to cart!';

  @override
  String get description => 'Description';

  @override
  String get mapView => 'Map View';

  @override
  String get mapPlaceholder => 'Google Maps will be integrated here';

  @override
  String get nearbyStores => 'Nearby Stores';

  @override
  String get filter => 'Filter';

  @override
  String get searchStoresHint => 'Search stores...';

  @override
  String get couldNotLoadStores => 'Could not load stores';

  @override
  String get user => 'User';

  @override
  String get defaultEmail => 'user@example.com';

  @override
  String get account => 'Account';

  @override
  String get editProfile => 'Edit Profile';

  @override
  String get savedAddresses => 'Saved Addresses';

  @override
  String get favoriteStores => 'Favorite Stores';

  @override
  String get paymentMethods => 'Payment Methods';

  @override
  String get preferences => 'Preferences';

  @override
  String get notifications => 'Notifications';

  @override
  String get language => 'Language';

  @override
  String get darkMode => 'Dark Mode';

  @override
  String get support => 'Support';

  @override
  String get helpCenter => 'Help Center';

  @override
  String get about => 'About';

  @override
  String get signOut => 'Sign Out';

  @override
  String get merchantSignIn => 'Merchant Sign In';

  @override
  String get merchantSignInSubtitle => 'Manage your store and offers';

  @override
  String get businessEmailHint => 'Enter your business email';

  @override
  String get emailRequired => 'Email is required';

  @override
  String get invalidEmail => 'Invalid email';

  @override
  String get passwordRequired => 'Password is required';

  @override
  String get passwordMinChars => 'At least 6 characters';

  @override
  String get merchantForgotPassword => 'Forgot Password?';

  @override
  String get noMerchantAccount => 'Don\'t have a merchant account?';

  @override
  String get merchantSignUp => 'Sign Up';

  @override
  String get failedToLoadDashboard => 'Failed to load dashboard';

  @override
  String get online => 'Online';

  @override
  String get offline => 'Offline';

  @override
  String activeOffersCount(int count) {
    return '$count active offers';
  }

  @override
  String get todaysRevenue => 'Today\'s Revenue';

  @override
  String get orders => 'Orders';

  @override
  String get quickActions => 'Quick Actions';

  @override
  String get createOffer => 'Create Offer';

  @override
  String get viewOffers => 'View Offers';

  @override
  String get reports => 'Reports';

  @override
  String get recentOrders => 'Recent Orders';

  @override
  String get viewAll => 'View All';

  @override
  String get myOffers => 'My Offers';

  @override
  String get failedToLoadOffers => 'Failed to load offers';

  @override
  String get retry => 'Retry';

  @override
  String get inactive => 'Inactive';

  @override
  String get newTab => 'New';

  @override
  String get preparing => 'Preparing';

  @override
  String get ready => 'Ready';

  @override
  String order(String id) {
    return 'Order #$id';
  }

  @override
  String get items => 'Items';

  @override
  String itemsCount(int count) {
    return '$count items';
  }

  @override
  String get markAsReady => 'Mark as Ready';

  @override
  String get contactCustomer => 'Contact Customer';

  @override
  String get pendingPreparation => 'Pending Preparation';

  @override
  String get orderPlacedTime => 'Order placed 12 min ago';

  @override
  String get customer => 'Customer';

  @override
  String get customerName => 'John Doe';

  @override
  String get customerEmail => 'john@example.com';

  @override
  String get timeline => 'Timeline';

  @override
  String get readyForPickup => 'Ready for Pickup';

  @override
  String get pickedUp => 'Picked Up';

  @override
  String get offerTitle => 'Offer Title';

  @override
  String get offerTitleHint => 'e.g. Mixed Sushi Box';

  @override
  String get required => 'Required';

  @override
  String get descriptionHint => 'Describe the offer details';

  @override
  String get category => 'Category';

  @override
  String get originalPrice => 'Original Price';

  @override
  String get discountPrice => 'Discount Price';

  @override
  String get quantityAvailable => 'Quantity Available';

  @override
  String get quantityHint => 'Number of items';

  @override
  String get pickupWindow => 'Pickup Window';

  @override
  String get startTime => 'Start Time';

  @override
  String get endTime => 'End Time';

  @override
  String get addPhoto => 'Add Photo';

  @override
  String get uploadImageHint => 'Tap to upload an image';

  @override
  String get food => 'Food';

  @override
  String get bakery => 'Bakery';

  @override
  String get desserts => 'Desserts';

  @override
  String get groceries => 'Groceries';

  @override
  String get beverages => 'Beverages';

  @override
  String get reportsTitle => 'Reports';

  @override
  String get failedToLoadReports => 'Failed to load reports';

  @override
  String get today => 'Today';

  @override
  String get thisWeek => 'This Week';

  @override
  String get thisMonth => 'This Month';

  @override
  String get totalRevenue => 'Total Revenue';

  @override
  String ordersCompleted(int count) {
    return '$count orders completed';
  }

  @override
  String get avgOrder => 'Avg. Order';

  @override
  String get itemsSaved => 'Items Saved';

  @override
  String get rating => 'Rating';

  @override
  String get customers => 'Customers';

  @override
  String get topSellingItems => 'Top Selling Items';

  @override
  String get noDataYet => 'No data yet';

  @override
  String sold(int count) {
    return '$count sold';
  }

  @override
  String get recentTransactions => 'Recent Transactions';

  @override
  String get noTransactionsYet => 'No transactions yet';

  @override
  String get somethingWentWrong => 'Something went wrong';

  @override
  String get tryAgain => 'Try Again';

  @override
  String get noInternetConnection => 'No internet connection';

  @override
  String get internetSubtitle => 'Check your connection and try again.';

  @override
  String get serverError => 'Server error';

  @override
  String get serverSubtitle =>
      'Our servers are having trouble. Please try again later.';

  @override
  String get notFound => 'Not found';

  @override
  String get notFoundSubtitle =>
      'The page you\'re looking for doesn\'t exist or has been removed.';

  @override
  String get goBack => 'Go Back';

  @override
  String get locationRequired => 'Location required';

  @override
  String get locationSubtitle =>
      'Enable location services to find nearby offers.';

  @override
  String get enableLocation => 'Enable Location';

  @override
  String get dismiss => 'Dismiss';

  @override
  String get noFavoritesTitle => 'No favorites yet';

  @override
  String get noFavoritesSubtitle =>
      'Save your favorite stores and offers for quick access later.';

  @override
  String get discoverStores => 'Discover Stores';

  @override
  String get noResultsTitle => 'No results found';

  @override
  String get noResultsSubtitle =>
      'We couldn\'t find what you\'re looking for. Try a different search term.';

  @override
  String get noOffersTitle => 'No offers available';

  @override
  String get noOffersSubtitle =>
      'There are no offers right now. Check back later for new deals!';

  @override
  String get navHome => 'Home';

  @override
  String get navMap => 'Map';

  @override
  String get navCart => 'Cart';

  @override
  String get navOrders => 'Orders';

  @override
  String get navProfile => 'Profile';

  @override
  String get navDashboard => 'Dashboard';

  @override
  String get navOffers => 'Offers';

  @override
  String get navReports => 'Reports';

  @override
  String fieldRequired(String fieldName) {
    return '$fieldName is required';
  }

  @override
  String get validEmail => 'Please enter a valid email address';

  @override
  String get passwordMinLength => 'Password must be at least 8 characters';

  @override
  String get passwordUppercase =>
      'Password must contain at least one uppercase letter';

  @override
  String get passwordLowercase =>
      'Password must contain at least one lowercase letter';

  @override
  String get passwordNumber => 'Password must contain at least one number';

  @override
  String get confirmPasswordRequired => 'Please confirm your password';

  @override
  String get passwordsNotMatch => 'Passwords do not match';

  @override
  String get phoneRequired => 'Phone number is required';

  @override
  String get validPhone => 'Please enter a valid phone number';

  @override
  String get otpRequired => 'OTP is required';

  @override
  String get validOtp => 'Please enter a valid 6-digit OTP';

  @override
  String get priceRequired => 'Price is required';

  @override
  String get validPrice => 'Please enter a valid price';

  @override
  String get pricePositive => 'Price must be greater than zero';

  @override
  String get quantityRequired => 'Quantity is required';

  @override
  String get validQuantity => 'Please enter a valid quantity';

  @override
  String get nameRequired => 'Name is required';

  @override
  String get nameMinLength => 'Name must be at least 2 characters';

  @override
  String get addressRequired => 'Address is required';

  @override
  String get addressComplete => 'Please enter a complete address';

  @override
  String get validUrl => 'Please enter a valid URL';

  @override
  String valueRequired(String fieldName) {
    return '$fieldName is required';
  }

  @override
  String get validNumber => 'Please enter a valid number';

  @override
  String noNegative(String fieldName) {
    return '$fieldName cannot be negative';
  }

  @override
  String get justNow => 'Just now';

  @override
  String minutesAgo(int count) {
    return '${count}m ago';
  }

  @override
  String hoursAgo(int count) {
    return '${count}h ago';
  }

  @override
  String daysAgo(int count) {
    return '${count}d ago';
  }

  @override
  String weeksAgo(int count) {
    return '${count}w ago';
  }

  @override
  String monthsAgo(int count) {
    return '${count}mo ago';
  }

  @override
  String yearsAgo(int count) {
    return '${count}y ago';
  }

  @override
  String get statusPending => 'Pending';

  @override
  String get statusConfirmed => 'Confirmed';

  @override
  String get statusPreparing => 'Preparing';

  @override
  String get statusReadyForPickup => 'Ready for Pickup';

  @override
  String get statusPickedUp => 'Picked Up';

  @override
  String get statusDelivered => 'Delivered';

  @override
  String get statusCancelled => 'Cancelled';

  @override
  String get statusRejected => 'Rejected';

  @override
  String get expired => 'Expired';

  @override
  String get closed => 'Closed';

  @override
  String get open => 'Open';

  @override
  String get km => 'km';

  @override
  String offers(int count) {
    return '$count offers';
  }

  @override
  String addToCartWithPrice(String price) {
    return 'Add to Cart - $price';
  }

  @override
  String orderNumberShort(String number) {
    return '#$number';
  }
}
