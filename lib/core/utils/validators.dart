import 'package:flutter/material.dart';
import 'package:last_hour/l10n/app_localizations.dart';

class Validators {
  Validators._();

  static String? required(String? value, [String fieldName = 'This field', BuildContext? context]) {
    if (value == null || value.trim().isEmpty) {
      final loc = context != null ? AppLocalizations.of(context) : null;
      return loc?.fieldRequired(fieldName) ?? '$fieldName is required';
    }
    return null;
  }

  static String? email(String? value, [BuildContext? context]) {
    final loc = context != null ? AppLocalizations.of(context) : null;
    if (value == null || value.trim().isEmpty) {
      return loc?.emailRequired ?? 'Email is required';
    }
    final emailRegex = RegExp(
      r'^[a-zA-Z0-9.]+@[a-zA-Z0-9]+\.[a-zA-Z]+',
    );
    if (!emailRegex.hasMatch(value.trim())) {
      return loc?.validEmail ?? 'Please enter a valid email address';
    }
    return null;
  }

  static String? password(String? value, [BuildContext? context]) {
    final loc = context != null ? AppLocalizations.of(context) : null;
    if (value == null || value.trim().isEmpty) {
      return loc?.passwordRequired ?? 'Password is required';
    }
    if (value.length < 8) {
      return loc?.passwordMinLength ?? 'Password must be at least 8 characters';
    }
    if (!value.contains(RegExp(r'[A-Z]'))) {
      return loc?.passwordUppercase ?? 'Password must contain at least one uppercase letter';
    }
    if (!value.contains(RegExp(r'[a-z]'))) {
      return loc?.passwordLowercase ?? 'Password must contain at least one lowercase letter';
    }
    if (!value.contains(RegExp(r'[0-9]'))) {
      return loc?.passwordNumber ?? 'Password must contain at least one number';
    }
    return null;
  }

  static String? confirmPassword(String? value, String password, [BuildContext? context]) {
    final loc = context != null ? AppLocalizations.of(context) : null;
    if (value == null || value.trim().isEmpty) {
      return loc?.confirmPasswordRequired ?? 'Please confirm your password';
    }
    if (value != password) {
      return loc?.passwordsNotMatch ?? 'Passwords do not match';
    }
    return null;
  }

  static String? phone(String? value, [BuildContext? context]) {
    final loc = context != null ? AppLocalizations.of(context) : null;
    if (value == null || value.trim().isEmpty) {
      return loc?.phoneRequired ?? 'Phone number is required';
    }
    final phoneRegex = RegExp(r'^\+?[\d\s\-()]{7,15}$');
    if (!phoneRegex.hasMatch(value.trim())) {
      return loc?.validPhone ?? 'Please enter a valid phone number';
    }
    return null;
  }

  static String? otp(String? value, [BuildContext? context]) {
    final loc = context != null ? AppLocalizations.of(context) : null;
    if (value == null || value.trim().isEmpty) {
      return loc?.otpRequired ?? 'OTP is required';
    }
    if (value.length != 6 || !RegExp(r'^\d{6}$').hasMatch(value)) {
      return loc?.validOtp ?? 'Please enter a valid 6-digit OTP';
    }
    return null;
  }

  static String? price(String? value, [BuildContext? context]) {
    final loc = context != null ? AppLocalizations.of(context) : null;
    if (value == null || value.trim().isEmpty) {
      return loc?.priceRequired ?? 'Price is required';
    }
    final priceRegex = RegExp(r'^\d+(\.\d{1,2})?$');
    if (!priceRegex.hasMatch(value.trim())) {
      return loc?.validPrice ?? 'Please enter a valid price';
    }
    final priceValue = double.tryParse(value.trim());
    if (priceValue != null && priceValue <= 0) {
      return loc?.pricePositive ?? 'Price must be greater than zero';
    }
    return null;
  }

  static String? quantity(String? value, [BuildContext? context]) {
    final loc = context != null ? AppLocalizations.of(context) : null;
    if (value == null || value.trim().isEmpty) {
      return loc?.quantityRequired ?? 'Quantity is required';
    }
    final quantityValue = int.tryParse(value.trim());
    if (quantityValue == null || quantityValue <= 0) {
      return loc?.validQuantity ?? 'Please enter a valid quantity';
    }
    return null;
  }

  static String? name(String? value, [BuildContext? context]) {
    final loc = context != null ? AppLocalizations.of(context) : null;
    if (value == null || value.trim().isEmpty) {
      return loc?.nameRequired ?? 'Name is required';
    }
    if (value.trim().length < 2) {
      return loc?.nameMinLength ?? 'Name must be at least 2 characters';
    }
    return null;
  }

  static String? address(String? value, [BuildContext? context]) {
    final loc = context != null ? AppLocalizations.of(context) : null;
    if (value == null || value.trim().isEmpty) {
      return loc?.addressRequired ?? 'Address is required';
    }
    if (value.trim().length < 10) {
      return loc?.addressComplete ?? 'Please enter a complete address';
    }
    return null;
  }

  static String? url(String? value, [BuildContext? context]) {
    final loc = context != null ? AppLocalizations.of(context) : null;
    if (value == null || value.trim().isEmpty) {
      return null;
    }
    final uri = Uri.tryParse(value.trim());
    if (uri == null || !uri.isAbsolute) {
      return loc?.validUrl ?? 'Please enter a valid URL';
    }
    return null;
  }

  static String? positiveNumber(String? value, [String fieldName = 'Value', BuildContext? context]) {
    final loc = context != null ? AppLocalizations.of(context) : null;
    if (value == null || value.trim().isEmpty) {
      return loc?.valueRequired(fieldName) ?? '$fieldName is required';
    }
    final numberValue = double.tryParse(value.trim());
    if (numberValue == null) {
      return loc?.validNumber ?? 'Please enter a valid number';
    }
    if (numberValue < 0) {
      return loc?.noNegative(fieldName) ?? '$fieldName cannot be negative';
    }
    return null;
  }
}
