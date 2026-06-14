// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Arabic (`ar`).
class AppLocalizationsAr extends AppLocalizations {
  AppLocalizationsAr([String locale = 'ar']) : super(locale);

  @override
  String get appName => 'لست آور';

  @override
  String get appTagline => 'وفّر الطعام. وفّر المال. أنقذ الكوكب.';

  @override
  String get onboardingTitle1 => 'أنقذ الطعام الفائض';

  @override
  String get onboardingSubtitle1 =>
      'اكتشف وجبات شهية من المطاعم والمخابز والأسواق المحلية بأسعار مخفضة.';

  @override
  String get onboardingTitle2 => 'خصومات رائعة';

  @override
  String get onboardingSubtitle2 =>
      'احصل على خصم يصل إلى ٧٠٪ على الطعام الطازج الذي كان سيهدر. رائع لمحفظتك وللكوكب!';

  @override
  String get onboardingTitle3 => 'حارب هدر الطعام';

  @override
  String get onboardingSubtitle3 =>
      'انضم إلى آلاف الأشخاص الذين ينقذون الطعام من الهدر. كل وجبة يتم إنقاذها تُحدث فرقاً.';

  @override
  String get onboardingSkip => 'تخطي';

  @override
  String get onboardingGetStarted => 'ابدأ الآن';

  @override
  String get onboardingNext => 'التالي';

  @override
  String get welcomeBack => 'مرحباً بعودتك!';

  @override
  String get signInSubtitle => 'سجّل الدخول لتوفير الطعام والمال';

  @override
  String get email => 'البريد الإلكتروني';

  @override
  String get emailHint => 'أدخل بريدك الإلكتروني';

  @override
  String get password => 'كلمة المرور';

  @override
  String get passwordHint => 'أدخل كلمة المرور';

  @override
  String get forgotPassword => 'نسيت كلمة المرور؟';

  @override
  String get signIn => 'تسجيل الدخول';

  @override
  String get orContinueWith => 'أو تابع بواسطة';

  @override
  String get noAccount => 'ليس لديك حساب؟ ';

  @override
  String get signUp => 'إنشاء حساب';

  @override
  String get createAccount => 'إنشاء حساب';

  @override
  String get registerSubtitle => 'انضم لمكافحة هدر الطعام';

  @override
  String get fullName => 'الاسم الكامل';

  @override
  String get fullNameHint => 'أدخل اسمك الكامل';

  @override
  String get phoneOptional => 'الهاتف (اختياري)';

  @override
  String get phoneHint => 'أدخل رقم هاتفك';

  @override
  String get createStrongPassword => 'أنشئ كلمة مرور قوية';

  @override
  String get confirmPassword => 'تأكيد كلمة المرور';

  @override
  String get confirmPasswordHint => 'أكد كلمة المرور';

  @override
  String get orSignUpWith => 'أو اشترك بواسطة';

  @override
  String get alreadyHaveAccount => 'لديك حساب بالفعل؟ ';

  @override
  String get forgotPasswordTitle => 'نسيت كلمة المرور؟';

  @override
  String get forgotPasswordSubtitle =>
      'أدخل بريدك الإلكتروني وسنرسل لك رمز التحقق لإعادة تعيين كلمة المرور.';

  @override
  String get sendOtp => 'إرسال الرمز';

  @override
  String get verifyOtpTitle => 'تحقق من الرمز';

  @override
  String verifyOtpSubtitle(String email) {
    return 'أدخل الرمز المكون من ٦ أرقام المرسل إلى $email';
  }

  @override
  String get verify => 'تحقق';

  @override
  String get didntReceiveCode => 'لم تستلم الرمز؟ ';

  @override
  String get resend => 'إعادة إرسال';

  @override
  String get deliverTo => 'التوصيل إلى';

  @override
  String get currentLocation => 'الموقع الحالي';

  @override
  String get orderBy9pm => 'اطلب قبل الساعة ٩ مساءً للاستلام';

  @override
  String get searchHint => 'ابحث عن عروض، متاجر...';

  @override
  String get categories => 'التصنيفات';

  @override
  String get featuredOffers => 'عروض مميزة';

  @override
  String get seeAll => 'عرض الكل';

  @override
  String get nearbyOffers => 'عروض قريبة';

  @override
  String get noOffersInCategory => 'لا توجد عروض في هذا التصنيف بالقرب منك';

  @override
  String get viewAllOffers => 'عرض كل العروض';

  @override
  String get myCart => 'سلة المشتريات';

  @override
  String get clear => 'مسح';

  @override
  String get subtotal => 'المجموع الفرعي';

  @override
  String get serviceFee => 'رسوم الخدمة';

  @override
  String get total => 'الإجمالي';

  @override
  String get proceedToCheckout => 'متابعة الدفع';

  @override
  String get cartEmptyTitle => 'سلتك فارغة';

  @override
  String get cartEmptySubtitle =>
      'يبدو أنك لم تُضف أي شيء بعد. تصفح العروض القريبة للعثور على صفقات رائعة!';

  @override
  String get browseOffers => 'تصفح العروض';

  @override
  String get myOrders => 'طلباتي';

  @override
  String get active => 'نشط';

  @override
  String get completed => 'مكتمل';

  @override
  String get cancelled => 'ملغي';

  @override
  String get couldNotLoadOrders => 'تعذر تحميل الطلبات';

  @override
  String itemCount(int count) {
    return '$count عنصر';
  }

  @override
  String get noOrdersTitle => 'لا توجد طلبات بعد';

  @override
  String get noOrdersSubtitle =>
      'لم تقم بأي طلبات بعد. ابدأ الاستكشاف وأنقذ الطعام من الهدر!';

  @override
  String get exploreOffers => 'استكشف العروض';

  @override
  String get checkout => 'الدفع';

  @override
  String get delivery => 'التوصيل';

  @override
  String get payment => 'الدفع';

  @override
  String get review => 'المراجعة';

  @override
  String get deliveryMode => 'طريقة التوصيل';

  @override
  String get pickup => 'استلام';

  @override
  String get deliveryAddress => 'عنوان التوصيل';

  @override
  String get home => 'المنزل';

  @override
  String get defaultAddress => '١٢٣ الشارع الرئيسي، وسط المدينة';

  @override
  String get paymentMethod => 'طريقة الدفع';

  @override
  String get cashOnPickup => 'الدفع عند الاستلام';

  @override
  String get creditCard => 'بطاقة ائتمان';

  @override
  String get paypal => 'باي بال';

  @override
  String get orderSummary => 'ملخص الطلب';

  @override
  String qty(int quantity) {
    return 'الكمية: $quantity';
  }

  @override
  String get deliveryFree => 'مجاني';

  @override
  String get continueAction => 'متابعة';

  @override
  String get placeOrder => 'تأكيد الطلب';

  @override
  String get couldNotLoadCart => 'تعذر تحميل السلة';

  @override
  String get orderPlaced => 'تم الطلب';

  @override
  String get orderPlacedSubtitle => 'تم تقديم طلبك بنجاح';

  @override
  String get trackOrder => 'تتبع الطلب';

  @override
  String get backToHome => 'العودة إلى الرئيسية';

  @override
  String orderNumber(String number) {
    return 'طلب رقم $number';
  }

  @override
  String get store => 'المتجر';

  @override
  String get pickupTime => 'وقت الاستلام';

  @override
  String get orderTotal => 'الإجمالي';

  @override
  String get failedToLoadOffer => 'تعذر تحميل العرض';

  @override
  String get noDescription => 'لا يوجد وصف متاح.';

  @override
  String remainingLeft(int count) {
    return 'بقيت $count';
  }

  @override
  String get addToCart => 'أضف إلى السلة';

  @override
  String get addedToCart => 'تمت الإضافة إلى السلة!';

  @override
  String get description => 'الوصف';

  @override
  String get mapView => 'عرض الخريطة';

  @override
  String get mapPlaceholder => 'سيتم دمج خرائط جوجل هنا';

  @override
  String get nearbyStores => 'المتاجر القريبة';

  @override
  String get filter => 'تصفية';

  @override
  String get searchStoresHint => 'ابحث عن متاجر...';

  @override
  String get couldNotLoadStores => 'تعذر تحميل المتاجر';

  @override
  String get user => 'مستخدم';

  @override
  String get defaultEmail => 'user@example.com';

  @override
  String get account => 'الحساب';

  @override
  String get editProfile => 'تعديل الملف الشخصي';

  @override
  String get savedAddresses => 'العناوين المحفوظة';

  @override
  String get favoriteStores => 'المتاجر المفضلة';

  @override
  String get paymentMethods => 'طرق الدفع';

  @override
  String get preferences => 'التفضيلات';

  @override
  String get notifications => 'الإشعارات';

  @override
  String get language => 'اللغة';

  @override
  String get darkMode => 'الوضع الليلي';

  @override
  String get support => 'الدعم';

  @override
  String get helpCenter => 'مركز المساعدة';

  @override
  String get about => 'حول';

  @override
  String get signOut => 'تسجيل الخروج';

  @override
  String get merchantSignIn => 'تسجيل دخول التاجر';

  @override
  String get merchantSignInSubtitle => 'إدارة متجرك وعروضك';

  @override
  String get businessEmailHint => 'أدخل بريدك التجاري الإلكتروني';

  @override
  String get emailRequired => 'البريد الإلكتروني مطلوب';

  @override
  String get invalidEmail => 'بريد إلكتروني غير صالح';

  @override
  String get passwordRequired => 'كلمة المرور مطلوبة';

  @override
  String get passwordMinChars => '6 أحرف على الأقل';

  @override
  String get merchantForgotPassword => 'نسيت كلمة المرور؟';

  @override
  String get noMerchantAccount => 'ليس لديك حساب تاجر؟';

  @override
  String get merchantSignUp => 'اشترك الآن';

  @override
  String get failedToLoadDashboard => 'تعذر تحميل لوحة التحكم';

  @override
  String get online => 'متصل';

  @override
  String get offline => 'غير متصل';

  @override
  String activeOffersCount(int count) {
    return '$count عروض نشطة';
  }

  @override
  String get todaysRevenue => 'إيرادات اليوم';

  @override
  String get orders => 'الطلبات';

  @override
  String get quickActions => 'إجراءات سريعة';

  @override
  String get createOffer => 'إنشاء عرض';

  @override
  String get viewOffers => 'عرض العروض';

  @override
  String get reports => 'التقارير';

  @override
  String get recentOrders => 'الطلبات الأخيرة';

  @override
  String get viewAll => 'عرض الكل';

  @override
  String get myOffers => 'عروضي';

  @override
  String get failedToLoadOffers => 'تعذر تحميل العروض';

  @override
  String get retry => 'إعادة المحاولة';

  @override
  String get inactive => 'غير نشط';

  @override
  String get newTab => 'جديد';

  @override
  String get preparing => 'قيد التحضير';

  @override
  String get ready => 'جاهز';

  @override
  String order(String id) {
    return 'Order #$id';
  }

  @override
  String get items => 'العناصر';

  @override
  String itemsCount(int count) {
    return '$count عناصر';
  }

  @override
  String get markAsReady => 'تحديد كجاهز';

  @override
  String get contactCustomer => 'تواصل مع العميل';

  @override
  String get pendingPreparation => 'بانتظار التحضير';

  @override
  String get orderPlacedTime => 'تم الطلب منذ ١٢ دقيقة';

  @override
  String get customer => 'العميل';

  @override
  String get customerName => 'محمد أحمد';

  @override
  String get customerEmail => 'mohamed@example.com';

  @override
  String get timeline => 'الجدول الزمني';

  @override
  String get readyForPickup => 'جاهز للاستلام';

  @override
  String get pickedUp => 'تم الاستلام';

  @override
  String get offerTitle => 'عنوان العرض';

  @override
  String get offerTitleHint => 'مثال: صندوق سوشي مشكل';

  @override
  String get required => 'مطلوب';

  @override
  String get descriptionHint => 'صف تفاصيل العرض';

  @override
  String get category => 'التصنيف';

  @override
  String get originalPrice => 'السعر الأصلي';

  @override
  String get discountPrice => 'السعر بعد الخصم';

  @override
  String get quantityAvailable => 'الكمية المتاحة';

  @override
  String get quantityHint => 'عدد العناصر';

  @override
  String get pickupWindow => 'وقت الاستلام';

  @override
  String get startTime => 'وقت البداية';

  @override
  String get endTime => 'وقت النهاية';

  @override
  String get addPhoto => 'إضافة صورة';

  @override
  String get uploadImageHint => 'اضغط لرفع صورة';

  @override
  String get food => 'طعام';

  @override
  String get bakery => 'مخبوزات';

  @override
  String get desserts => 'حلويات';

  @override
  String get groceries => 'بقالة';

  @override
  String get beverages => 'مشروبات';

  @override
  String get reportsTitle => 'التقارير';

  @override
  String get failedToLoadReports => 'تعذر تحميل التقارير';

  @override
  String get today => 'اليوم';

  @override
  String get thisWeek => 'هذا الأسبوع';

  @override
  String get thisMonth => 'هذا الشهر';

  @override
  String get totalRevenue => 'إجمالي الإيرادات';

  @override
  String ordersCompleted(int count) {
    return '$count طلب مكتمل';
  }

  @override
  String get avgOrder => 'متوسط الطلب';

  @override
  String get itemsSaved => 'عناصر تم إنقاذها';

  @override
  String get rating => 'التقييم';

  @override
  String get customers => 'العملاء';

  @override
  String get topSellingItems => 'الأكثر مبيعاً';

  @override
  String get noDataYet => 'لا توجد بيانات بعد';

  @override
  String sold(int count) {
    return '$count مباع';
  }

  @override
  String get recentTransactions => 'المعاملات الأخيرة';

  @override
  String get noTransactionsYet => 'لا توجد معاملات بعد';

  @override
  String get somethingWentWrong => 'حدث خطأ ما';

  @override
  String get tryAgain => 'حاول مرة أخرى';

  @override
  String get noInternetConnection => 'لا يوجد اتصال بالإنترنت';

  @override
  String get internetSubtitle => 'تحقق من اتصالك وحاول مرة أخرى.';

  @override
  String get serverError => 'خطأ في الخادم';

  @override
  String get serverSubtitle => 'الخوادم تواجه مشكلة. يرجى المحاولة لاحقاً.';

  @override
  String get notFound => 'غير موجود';

  @override
  String get notFoundSubtitle =>
      'الصفحة التي تبحث عنها غير موجودة أو تمت إزالتها.';

  @override
  String get goBack => 'العودة';

  @override
  String get locationRequired => 'الموقع مطلوب';

  @override
  String get locationSubtitle => 'فعّل خدمات الموقع للعثور على العروض القريبة.';

  @override
  String get enableLocation => 'تفعيل الموقع';

  @override
  String get dismiss => 'تجاهل';

  @override
  String get noFavoritesTitle => 'لا توجد مفضلات بعد';

  @override
  String get noFavoritesSubtitle =>
      'احفظ متاجرك وعروضك المفضلة للوصول السريع لاحقاً.';

  @override
  String get discoverStores => 'اكتشف المتاجر';

  @override
  String get noResultsTitle => 'لا توجد نتائج';

  @override
  String get noResultsSubtitle =>
      'لم نعثر على ما تبحث عنه. جرب كلمة بحث مختلفة.';

  @override
  String get noOffersTitle => 'لا توجد عروض متاحة';

  @override
  String get noOffersSubtitle =>
      'لا توجد عروض الآن. تحقق لاحقاً للحصول على عروض جديدة!';

  @override
  String get navHome => 'الرئيسية';

  @override
  String get navMap => 'الخريطة';

  @override
  String get navCart => 'السلة';

  @override
  String get navOrders => 'الطلبات';

  @override
  String get navProfile => 'الملف الشخصي';

  @override
  String get navDashboard => 'لوحة التحكم';

  @override
  String get navOffers => 'العروض';

  @override
  String get navReports => 'التقارير';

  @override
  String fieldRequired(String fieldName) {
    return '$fieldName مطلوب';
  }

  @override
  String get validEmail => 'يرجى إدخال بريد إلكتروني صالح';

  @override
  String get passwordMinLength => 'كلمة المرور يجب أن تكون 8 أحرف على الأقل';

  @override
  String get passwordUppercase =>
      'يجب أن تحتوي كلمة المرور على حرف كبير واحد على الأقل';

  @override
  String get passwordLowercase =>
      'يجب أن تحتوي كلمة المرور على حرف صغير واحد على الأقل';

  @override
  String get passwordNumber =>
      'يجب أن تحتوي كلمة المرور على رقم واحد على الأقل';

  @override
  String get confirmPasswordRequired => 'يرجى تأكيد كلمة المرور';

  @override
  String get passwordsNotMatch => 'كلمتا المرور غير متطابقتين';

  @override
  String get phoneRequired => 'رقم الهاتف مطلوب';

  @override
  String get validPhone => 'يرجى إدخال رقم هاتف صالح';

  @override
  String get otpRequired => 'رمز التحقق مطلوب';

  @override
  String get validOtp => 'يرجى إدخال رمز تحقق صحيح مكون من 6 أرقام';

  @override
  String get priceRequired => 'السعر مطلوب';

  @override
  String get validPrice => 'يرجى إدخال سعر صالح';

  @override
  String get pricePositive => 'يجب أن يكون السعر أكبر من صفر';

  @override
  String get quantityRequired => 'الكمية مطلوبة';

  @override
  String get validQuantity => 'يرجى إدخال كمية صالحة';

  @override
  String get nameRequired => 'الاسم مطلوب';

  @override
  String get nameMinLength => 'يجب أن يتكون الاسم من حرفين على الأقل';

  @override
  String get addressRequired => 'العنوان مطلوب';

  @override
  String get addressComplete => 'يرجى إدخال عنوان كامل';

  @override
  String get validUrl => 'يرجى إدخال رابط صالح';

  @override
  String valueRequired(String fieldName) {
    return '$fieldName مطلوب';
  }

  @override
  String get validNumber => 'يرجى إدخال رقم صالح';

  @override
  String noNegative(String fieldName) {
    return '$fieldName لا يمكن أن يكون سالباً';
  }

  @override
  String get justNow => 'الآن';

  @override
  String minutesAgo(int count) {
    return 'منذ $count دقيقة';
  }

  @override
  String hoursAgo(int count) {
    return 'منذ $count ساعة';
  }

  @override
  String daysAgo(int count) {
    return 'منذ $count يوم';
  }

  @override
  String weeksAgo(int count) {
    return 'منذ $count أسبوع';
  }

  @override
  String monthsAgo(int count) {
    return 'منذ $count شهر';
  }

  @override
  String yearsAgo(int count) {
    return 'منذ $count سنة';
  }

  @override
  String get statusPending => 'قيد الانتظار';

  @override
  String get statusConfirmed => 'مؤكد';

  @override
  String get statusPreparing => 'قيد التحضير';

  @override
  String get statusReadyForPickup => 'جاهز للاستلام';

  @override
  String get statusPickedUp => 'تم الاستلام';

  @override
  String get statusDelivered => 'تم التوصيل';

  @override
  String get statusCancelled => 'ملغي';

  @override
  String get statusRejected => 'مرفوض';

  @override
  String get expired => 'منتهي';

  @override
  String get closed => 'مغلق';

  @override
  String get open => 'مفتوح';

  @override
  String get km => 'كم';

  @override
  String offers(int count) {
    return '$count عروض';
  }

  @override
  String addToCartWithPrice(String price) {
    return 'أضف إلى السلة - $price';
  }

  @override
  String orderNumberShort(String number) {
    return '#$number';
  }
}
