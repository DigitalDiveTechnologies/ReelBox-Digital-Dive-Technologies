import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/data/datasources/auth_local_datasource.dart';
import '../config/app_config.dart';
import 'api_client.dart';

/// Local token store (shared by auth + API client).
final authLocalDataSourceProvider = Provider<AuthLocalDataSource>((ref) {
  return AuthLocalDataSourceImpl();
});

/// Shared HTTP API client (JWT-aware).
final apiClientProvider = Provider<ApiClient>((ref) {
  final local = ref.watch(authLocalDataSourceProvider);

  return HttpApiClient(
    baseUrl: AppConfig.apiBaseUrl,
    readAccessToken: local.getAccessToken,
    readRefreshToken: local.getRefreshToken,
    saveTokens: ({
      required String accessToken,
      required String refreshToken,
    }) {
      return local.saveTokens(
        accessToken: accessToken,
        refreshToken: refreshToken,
      );
    },
    clearSession: local.clearSession,
  );
});
