import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

class AppTextStyles {
  AppTextStyles._();

  // Display
  static TextStyle displayLarge = TextStyle(
    fontSize: 57.sp,
    fontWeight: FontWeight.w700,
    height: 1.12,
    letterSpacing: -0.25,
  );

  static TextStyle displayMedium = TextStyle(
    fontSize: 45.sp,
    fontWeight: FontWeight.w700,
    height: 1.15,
    letterSpacing: 0,
  );

  static TextStyle displaySmall = TextStyle(
    fontSize: 36.sp,
    fontWeight: FontWeight.w600,
    height: 1.18,
    letterSpacing: 0,
  );

  // Headlines
  static TextStyle headlineLarge = TextStyle(
    fontSize: 32.sp,
    fontWeight: FontWeight.w700,
    height: 1.25,
    letterSpacing: 0,
  );

  static TextStyle headlineMedium = TextStyle(
    fontSize: 28.sp,
    fontWeight: FontWeight.w600,
    height: 1.29,
    letterSpacing: 0,
  );

  static TextStyle headlineSmall = TextStyle(
    fontSize: 24.sp,
    fontWeight: FontWeight.w600,
    height: 1.33,
    letterSpacing: 0,
  );

  // Titles
  static TextStyle titleLarge = TextStyle(
    fontSize: 22.sp,
    fontWeight: FontWeight.w600,
    height: 1.27,
    letterSpacing: 0,
  );

  static TextStyle titleMedium = TextStyle(
    fontSize: 16.sp,
    fontWeight: FontWeight.w600,
    height: 1.5,
    letterSpacing: 0.15,
  );

  static TextStyle titleSmall = TextStyle(
    fontSize: 14.sp,
    fontWeight: FontWeight.w600,
    height: 1.43,
    letterSpacing: 0.1,
  );

  // Labels
  static TextStyle labelLarge = TextStyle(
    fontSize: 14.sp,
    fontWeight: FontWeight.w500,
    height: 1.43,
    letterSpacing: 0.1,
  );

  static TextStyle labelMedium = TextStyle(
    fontSize: 12.sp,
    fontWeight: FontWeight.w500,
    height: 1.33,
    letterSpacing: 0.5,
  );

  static TextStyle labelSmall = TextStyle(
    fontSize: 11.sp,
    fontWeight: FontWeight.w500,
    height: 1.45,
    letterSpacing: 0.5,
  );

  // Body
  static TextStyle bodyLarge = TextStyle(
    fontSize: 16.sp,
    fontWeight: FontWeight.w400,
    height: 1.5,
    letterSpacing: 0.5,
  );

  static TextStyle bodyMedium = TextStyle(
    fontSize: 14.sp,
    fontWeight: FontWeight.w400,
    height: 1.43,
    letterSpacing: 0.25,
  );

  static TextStyle bodySmall = TextStyle(
    fontSize: 12.sp,
    fontWeight: FontWeight.w400,
    height: 1.33,
    letterSpacing: 0.4,
  );

  // Custom styles
  static TextStyle caption = TextStyle(
    fontSize: 12.sp,
    fontWeight: FontWeight.w400,
    height: 1.33,
    letterSpacing: 0.4,
    color: Colors.grey,
  );

  static TextStyle button = TextStyle(
    fontSize: 14.sp,
    fontWeight: FontWeight.w600,
    height: 1.43,
    letterSpacing: 0.5,
  );

  static TextStyle price = TextStyle(
    fontSize: 18.sp,
    fontWeight: FontWeight.w700,
    height: 1.28,
    letterSpacing: 0,
  );

  static TextStyle discount = TextStyle(
    fontSize: 12.sp,
    fontWeight: FontWeight.w700,
    height: 1.33,
    letterSpacing: 0,
  );

  static TextStyle countdown = TextStyle(
    fontSize: 13.sp,
    fontWeight: FontWeight.w700,
    height: 1.23,
    letterSpacing: 0.5,
  );

  static TextStyle productName = TextStyle(
    fontSize: 15.sp,
    fontWeight: FontWeight.w600,
    height: 1.4,
    letterSpacing: 0.15,
  );

  static TextStyle storeName = TextStyle(
    fontSize: 13.sp,
    fontWeight: FontWeight.w500,
    height: 1.38,
    letterSpacing: 0.25,
    color: Colors.grey,
  );
}
