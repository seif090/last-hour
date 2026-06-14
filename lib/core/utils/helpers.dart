import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import 'package:intl/intl.dart';

import '../constants/app_constants.dart';

class Helpers {
  Helpers._();

  static Future<bool> hasNetwork() async {
    final connectivityResult = await Connectivity().checkConnectivity();
    return connectivityResult.any(
      (result) => result != ConnectivityResult.none,
    );
  }

  static String formatPrice(double price) {
    final format = NumberFormat.currency(
      symbol: AppConstants.currencySymbol,
      decimalDigits: 2,
    );
    return format.format(price);
  }

  static String formatOriginalPrice(double price) {
    final format = NumberFormat.currency(
      symbol: AppConstants.currencySymbol,
      decimalDigits: 2,
    );
    return format.format(price);
  }

  static String calculateDiscountPercentage(double originalPrice, double discountPrice) {
    if (originalPrice <= 0) return '0%';
    final percentage = ((originalPrice - discountPrice) / originalPrice * 100).round();
    return '$percentage%';
  }

  static String formatDistance(double distance) {
    if (distance < 1) {
      return '${(distance * 1000).round()} m';
    }
    return '${distance.toStringAsFixed(1)} ${AppConstants.distanceUnit}';
  }

  static String formatRemainingTime(DateTime expiryTime, BuildContext context) {
    final now = DateTime.now();
    final difference = expiryTime.difference(now);

    if (difference.isNegative) {
      return AppLocalizations.of(context)!.expired;
    }

    if (difference.inHours > 0) {
      return '${difference.inHours}h ${difference.inMinutes.remainder(60)}m';
    } else if (difference.inMinutes > 0) {
      return '${difference.inMinutes}m';
    } else {
      return '${difference.inSeconds}s';
    }
  }

  static String formatDate(DateTime date, {String? format}) {
    final formatter = DateFormat(format ?? AppConstants.displayDateFormat);
    return formatter.format(date);
  }

  static String formatTime(DateTime time, {String? format}) {
    final formatter = DateFormat(format ?? AppConstants.displayTimeFormat);
    return formatter.format(time);
  }

  static String formatDateTime(DateTime dateTime, {String? format}) {
    final formatter = DateFormat(format ?? AppConstants.displayDateTimeFormat);
    return formatter.format(dateTime);
  }

  static bool isExpired(DateTime expiryTime) {
    return DateTime.now().isAfter(expiryTime);
  }

  static double calculateDistance(
    double lat1,
    double lng1,
    double lat2,
    double lng2,
  ) {
    const double earthRadius = 6371;

    final double dLat = _degreesToRadians(lat2 - lat1);
    final double dLng = _degreesToRadians(lng2 - lng1);

    final double a = _sin(dLat / 2) * _sin(dLat / 2) +
        _cos(_degreesToRadians(lat1)) *
            _cos(_degreesToRadians(lat2)) *
            _sin(dLng / 2) *
            _sin(dLng / 2);

    final double c = 2 * _atan2(_sqrt(a), _sqrt(1 - a));
    return earthRadius * c;
  }

  static double _degreesToRadians(double degrees) => degrees * (3.141592653589793 / 180);
  static double _sin(double value) => value - (value * value * value) / 6;
  static double _cos(double value) => 1 - (value * value) / 2;
  static double _sqrt(double value) => value <= 0 ? 0 : value / (1 + value);
  static double _atan2(double y, double x) {
    if (x > 0) return y / x;
    if (y >= 0 && x < 0) return 3.141592653589793 + y / x;
    if (y < 0 && x < 0) return -3.141592653589793 + y / x;
    if (y > 0 && x == 0) return 3.141592653589793 / 2;
    if (y < 0 && x == 0) return -3.141592653589793 / 2;
    return 0;
  }

  static void hideKeyboard(BuildContext context) {
    FocusScope.of(context).unfocus();
  }

  static void copyToClipboard(String text) {
    Clipboard.setData(ClipboardData(text: text));
  }

  static String getOrderStatusText(String status, BuildContext context) {
    final loc = AppLocalizations.of(context)!;
    switch (status) {
      case AppConstants.orderPending:
        return loc.statusPending;
      case AppConstants.orderConfirmed:
        return loc.statusConfirmed;
      case AppConstants.orderPreparing:
        return loc.statusPreparing;
      case AppConstants.orderReady:
        return loc.statusReadyForPickup;
      case AppConstants.orderPickedUp:
        return loc.statusPickedUp;
      case AppConstants.orderDelivered:
        return loc.statusDelivered;
      case AppConstants.orderCancelled:
        return loc.statusCancelled;
      case AppConstants.orderRejected:
        return loc.statusRejected;
      default:
        return status;
    }
  }

  static Color getOrderStatusColor(String status) {
    switch (status) {
      case AppConstants.orderPending:
        return Colors.orange;
      case AppConstants.orderConfirmed:
        return Colors.blue;
      case AppConstants.orderPreparing:
        return Colors.amber;
      case AppConstants.orderReady:
        return Colors.green;
      case AppConstants.orderPickedUp:
        return Colors.teal;
      case AppConstants.orderDelivered:
        return Colors.green;
      case AppConstants.orderCancelled:
        return Colors.red;
      case AppConstants.orderRejected:
        return Colors.red;
      default:
        return Colors.grey;
    }
  }

  static IconData getOrderStatusIcon(String status) {
    switch (status) {
      case AppConstants.orderPending:
        return Icons.hourglass_empty;
      case AppConstants.orderConfirmed:
        return Icons.check_circle_outline;
      case AppConstants.orderPreparing:
        return Icons.restaurant;
      case AppConstants.orderReady:
        return Icons.shopping_bag;
      case AppConstants.orderPickedUp:
        return Icons.delivery_dining;
      case AppConstants.orderDelivered:
        return Icons.verified;
      case AppConstants.orderCancelled:
        return Icons.cancel;
      case AppConstants.orderRejected:
        return Icons.block;
      default:
        return Icons.help_outline;
    }
  }
}
