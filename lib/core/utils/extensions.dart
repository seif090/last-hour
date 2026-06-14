import 'package:flutter/material.dart';
import 'package:last_hour/l10n/app_localizations.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

extension ContextExtensions on BuildContext {
  ThemeData get theme => Theme.of(this);
  TextTheme get textTheme => Theme.of(this).textTheme;
  ColorScheme get colorScheme => Theme.of(this).colorScheme;
  MediaQueryData get mediaQuery => MediaQuery.of(this);
  Size get screenSize => MediaQuery.of(this).size;
  double get screenWidth => MediaQuery.of(this).size.width;
  double get screenHeight => MediaQuery.of(this).size.height;
  double get statusBarHeight => MediaQuery.of(this).padding.top;
  double get bottomBarHeight => MediaQuery.of(this).padding.bottom;
  bool get isLandscape => MediaQuery.of(this).orientation == Orientation.landscape;
  bool get isPortrait => MediaQuery.of(this).orientation == Orientation.portrait;
  bool get isSmallScreen => screenWidth < 360;
  bool get isMediumScreen => screenWidth >= 360 && screenWidth < 600;
  bool get isLargeScreen => screenWidth >= 600;

  Future<T?> push<T>(Widget page) =>
      Navigator.of(this).push<T>(MaterialPageRoute(builder: (_) => page));

  Future<T?> pushReplacement<T>(Widget page) =>
      Navigator.of(this).pushReplacement<T, dynamic>(
        MaterialPageRoute(builder: (_) => page),
      );

  void pop<T>([T? result]) => Navigator.of(this).pop<T>(result);
}

extension NumExtensions on num {
  double get w => ScreenUtil().setWidth(this);
  double get h => ScreenUtil().setHeight(this);
  double get r => ScreenUtil().radius(this);
  double get sp => ScreenUtil().setSp(this);
  SizedBox get verticalSpace => SizedBox(height: toDouble());
  SizedBox get horizontalSpace => SizedBox(width: toDouble());
}

extension StringExtensions on String {
  String capitalize() {
    if (isEmpty) return this;
    return '${this[0].toUpperCase()}${substring(1)}';
  }

  String truncate(int maxLength) {
    if (length <= maxLength) return this;
    return '${substring(0, maxLength)}...';
  }

  bool get isValidEmail {
    final emailRegex = RegExp(r'^[a-zA-Z0-9.]+@[a-zA-Z0-9]+\.[a-zA-Z]+');
    return emailRegex.hasMatch(this);
  }

  bool get isValidPhone {
    final phoneRegex = RegExp(r'^\+?[\d\s\-()]{7,15}$');
    return phoneRegex.hasMatch(this);
  }

  String toTitleCase() {
    if (isEmpty) return this;
    return split(' ').map((word) => word.capitalize()).join(' ');
  }

  bool get isNumeric => double.tryParse(this) != null;
}

extension DateTimeExtensions on DateTime {
  bool get isToday {
    final now = DateTime.now();
    return year == now.year && month == now.month && day == now.day;
  }

  bool get isTomorrow {
    final tomorrow = DateTime.now().add(const Duration(days: 1));
    return year == tomorrow.year && month == tomorrow.month && day == tomorrow.day;
  }

  bool get isYesterday {
    final yesterday = DateTime.now().subtract(const Duration(days: 1));
    return year == yesterday.year && month == yesterday.month && day == yesterday.day;
  }

  bool isSameDay(DateTime other) {
    return year == other.year && month == other.month && day == other.day;
  }

  String timeAgo([BuildContext? context]) {
    final now = DateTime.now();
    final difference = now.difference(this);
    final loc = context != null ? AppLocalizations.of(context) : null;

    if (difference.inSeconds < 60) {
      return loc?.justNow ?? 'Just now';
    } else if (difference.inMinutes < 60) {
      return loc?.minutesAgo(difference.inMinutes) ?? '${difference.inMinutes}m ago';
    } else if (difference.inHours < 24) {
      return loc?.hoursAgo(difference.inHours) ?? '${difference.inHours}h ago';
    } else if (difference.inDays < 7) {
      return loc?.daysAgo(difference.inDays) ?? '${difference.inDays}d ago';
    } else if (difference.inDays < 30) {
      return loc?.weeksAgo((difference.inDays / 7).round()) ?? '${(difference.inDays / 7).round()}w ago';
    } else if (difference.inDays < 365) {
      return loc?.monthsAgo((difference.inDays / 30).round()) ?? '${(difference.inDays / 30).round()}mo ago';
    } else {
      return loc?.yearsAgo((difference.inDays / 365).round()) ?? '${(difference.inDays / 365).round()}y ago';
    }
  }
}

extension WidgetExtensions on Widget {
  Widget paddingAll(double value) =>
      Padding(padding: EdgeInsets.all(value), child: this);

  Widget paddingSymmetric({double horizontal = 0, double vertical = 0}) =>
      Padding(
        padding: EdgeInsets.symmetric(horizontal: horizontal, vertical: vertical),
        child: this,
      );

  Widget paddingOnly({
    double left = 0,
    double top = 0,
    double right = 0,
    double bottom = 0,
  }) => Padding(
    padding: EdgeInsets.only(left: left, top: top, right: right, bottom: bottom),
    child: this,
  );

  Widget onTap(VoidCallback onTap) => GestureDetector(onTap: onTap, child: this);

  Widget center() => Center(child: this);

  Widget expanded({int flex = 1}) => Expanded(flex: flex, child: this);
}

extension MapExtensions on Map<String, dynamic> {
  String? getString(String key) => this[key]?.toString();
  int? getInt(String key) => this[key] is int ? this[key] as int : int.tryParse(this[key]?.toString() ?? '');
  double? getDouble(String key) => this[key] is double ? this[key] as double : double.tryParse(this[key]?.toString() ?? '');
  bool? getBool(String key) => this[key] is bool ? this[key] as bool : (this[key]?.toString() == 'true');
  List<dynamic>? getList(String key) => this[key] is List ? this[key] as List : null;
  Map<String, dynamic>? getMap(String key) => this[key] is Map ? this[key] as Map<String, dynamic> : null;
}
