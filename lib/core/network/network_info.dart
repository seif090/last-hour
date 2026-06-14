import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';

class NetworkInfo {
  final Connectivity _connectivity;
  bool _isConnected = true;
  StreamSubscription<List<ConnectivityResult>>? _subscription;

  NetworkInfo({Connectivity? connectivity})
      : _connectivity = connectivity ?? Connectivity();

  Stream<bool> get onConnectivityChanged => _connectivity.onConnectivityChanged
      .map((results) => results.any((r) => r != ConnectivityResult.none));

  bool get isConnected => _isConnected;

  Future<bool> checkConnectivity() async {
    final results = await _connectivity.checkConnectivity();
    _isConnected = results.any((r) => r != ConnectivityResult.none);
    return _isConnected;
  }

  void initialize() {
    _subscription = _connectivity.onConnectivityChanged.listen((results) {
      _isConnected = results.any((r) => r != ConnectivityResult.none);
    });
  }

  void dispose() {
    _subscription?.cancel();
  }
}
