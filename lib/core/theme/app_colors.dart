import 'package:flutter/material.dart';
import '../constants/color_constants.dart';

class AppColors {
  AppColors._();

  static const Color primary = ColorConstants.primary;
  static const Color primaryLight = ColorConstants.primaryLight;
  static const Color primaryDark = ColorConstants.primaryDark;
  static const Color secondary = ColorConstants.secondary;
  static const Color secondaryLight = ColorConstants.secondaryLight;
  static const Color secondaryDark = ColorConstants.secondaryDark;
  static const Color tertiary = ColorConstants.tertiary;

  static const Color success = ColorConstants.success;
  static const Color warning = ColorConstants.warning;
  static const Color error = ColorConstants.error;
  static const Color info = ColorConstants.info;

  static const Color white = ColorConstants.white;
  static const Color black = ColorConstants.black;
  static const Color background = ColorConstants.background;
  static const Color surface = ColorConstants.surface;
  static const Color scaffoldBackground = ColorConstants.scaffoldBackground;

  static const Color grey50 = ColorConstants.grey50;
  static const Color grey100 = ColorConstants.grey100;
  static const Color grey200 = ColorConstants.grey200;
  static const Color grey300 = ColorConstants.grey300;
  static const Color grey400 = ColorConstants.grey400;
  static const Color grey500 = ColorConstants.grey500;
  static const Color grey600 = ColorConstants.grey600;
  static const Color grey700 = ColorConstants.grey700;
  static const Color grey800 = ColorConstants.grey800;
  static const Color grey900 = ColorConstants.grey900;

  static const Color discountRed = ColorConstants.discountRed;

  // Dark theme specific
  static const Color darkBackground = ColorConstants.darkBackground;
  static const Color darkSurface = ColorConstants.darkSurface;
  static const Color darkCardBackground = ColorConstants.darkCardBackground;
  static const Color darkScaffoldBackground = ColorConstants.darkScaffoldBackground;

  // Container colors
  static const Color primaryContainer = ColorConstants.primaryContainer;
  static const Color onPrimaryContainer = ColorConstants.onPrimaryContainer;
  static const Color secondaryContainer = ColorConstants.secondaryContainer;
  static const Color onSecondaryContainer = ColorConstants.onSecondaryContainer;

  // Light theme color scheme
  static const ColorScheme lightColorScheme = ColorScheme(
    brightness: Brightness.light,
    primary: primary,
    onPrimary: white,
    primaryContainer: ColorConstants.primaryContainer,
    onPrimaryContainer: ColorConstants.onPrimaryContainer,
    secondary: secondary,
    onSecondary: white,
    secondaryContainer: ColorConstants.secondaryContainer,
    onSecondaryContainer: ColorConstants.onSecondaryContainer,
    tertiary: tertiary,
    onTertiary: white,
    error: error,
    onError: white,
    surface: surface,
    onSurface: grey900,
    outline: grey300,
  );

  // Dark theme color scheme
  static const ColorScheme darkColorScheme = ColorScheme(
    brightness: Brightness.dark,
    primary: ColorConstants.primaryLight,
    onPrimary: ColorConstants.black,
    primaryContainer: ColorConstants.primaryDark,
    onPrimaryContainer: ColorConstants.primaryContainer,
    secondary: ColorConstants.secondaryLight,
    onSecondary: ColorConstants.black,
    secondaryContainer: ColorConstants.secondaryDark,
    onSecondaryContainer: ColorConstants.secondaryContainer,
    tertiary: ColorConstants.tertiaryLight,
    onTertiary: ColorConstants.black,
    error: ColorConstants.error,
    onError: ColorConstants.white,
    surface: ColorConstants.darkSurface,
    onSurface: ColorConstants.white,
    outline: ColorConstants.grey700,
  );
}
