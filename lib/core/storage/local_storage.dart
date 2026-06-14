import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../constants/app_constants.dart';

class LocalStorage {
  static LocalStorage? _instance;
  late SharedPreferences _prefs;

  LocalStorage._internal();

  static Future<LocalStorage> getInstance() async {
    if (_instance == null) {
      _instance = LocalStorage._internal();
      _instance!._prefs = await SharedPreferences.getInstance();
    }
    return _instance!;
  }

  Future<void> setOnboardingCompleted() async {
    await _prefs.setBool(AppConstants.onboardingKey, true);
  }

  bool isOnboardingCompleted() {
    return _prefs.getBool(AppConstants.onboardingKey) ?? false;
  }

  Future<void> setThemeMode(String mode) async {
    await _prefs.setString(AppConstants.themeModeKey, mode);
  }

  String? getThemeMode() {
    return _prefs.getString(AppConstants.themeModeKey);
  }

  Future<void> setLocale(String locale) async {
    await _prefs.setString(AppConstants.localeKey, locale);
  }

  String? getLocale() {
    return _prefs.getString(AppConstants.localeKey);
  }

  Future<void> saveCartCache(String cartJson) async {
    await _prefs.setString(AppConstants.cartCacheKey, cartJson);
  }

  String? getCartCache() {
    return _prefs.getString(AppConstants.cartCacheKey);
  }

  Future<void> clearCartCache() async {
    await _prefs.remove(AppConstants.cartCacheKey);
  }

  Future<void> saveString(String key, String value) async {
    await _prefs.setString(key, value);
  }

  String? getString(String key) {
    return _prefs.getString(key);
  }

  Future<void> saveBool(String key, bool value) async {
    await _prefs.setBool(key, value);
  }

  bool? getBool(String key) {
    return _prefs.getBool(key);
  }

  Future<void> saveInt(String key, int value) async {
    await _prefs.setInt(key, value);
  }

  int? getInt(String key) {
    return _prefs.getInt(key);
  }

  Future<void> saveDouble(String key, double value) async {
    await _prefs.setDouble(key, value);
  }

  double? getDouble(String key) {
    return _prefs.getDouble(key);
  }

  Future<void> saveStringList(String key, List<String> value) async {
    await _prefs.setStringList(key, value);
  }

  List<String>? getStringList(String key) {
    return _prefs.getStringList(key);
  }

  Future<void> saveObject(String key, dynamic value) async {
    final json = jsonEncode(value);
    await _prefs.setString(key, json);
  }

  dynamic getObject(String key) {
    final json = _prefs.getString(key);
    if (json == null) return null;
    return jsonDecode(json);
  }

  Future<void> remove(String key) async {
    await _prefs.remove(key);
  }

  Future<void> clear() async {
    await _prefs.clear();
  }

  bool containsKey(String key) {
    return _prefs.containsKey(key);
  }
}
