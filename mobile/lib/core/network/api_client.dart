import 'dart:convert';

import 'package:http/http.dart' as http;

import '../config/app_config.dart';
import '../errors/app_exception.dart';

/// Contract for JSON REST calls against the Social Reel Saver API.
abstract class ApiClient {
  Future<Map<String, dynamic>> getJson(
    String path, {
    Map<String, String>? queryParameters,
  });

  Future<Map<String, dynamic>> postJson(
    String path, {
    Object? body,
    Map<String, String>? queryParameters,
    bool authenticated = true,
  });

  Future<void> deleteJson(String path);
}

typedef AccessTokenReader = Future<String?> Function();
typedef RefreshTokenReader = Future<String?> Function();
typedef TokenSaver = Future<void> Function({
  required String accessToken,
  required String refreshToken,
});
typedef SessionClearer = Future<void> Function();

/// HTTP implementation of [ApiClient] with Bearer auth + one-shot refresh.
class HttpApiClient implements ApiClient {
  HttpApiClient({
    http.Client? httpClient,
    String? baseUrl,
    required this._readAccessToken,
    required this._readRefreshToken,
    required this._saveTokens,
    required this._clearSession,
  })  : _http = httpClient ?? http.Client(),
        _baseUrl =
            (baseUrl ?? AppConfig.apiBaseUrl).replaceAll(RegExp(r'/$'), '');

  final http.Client _http;
  final String _baseUrl;
  final AccessTokenReader _readAccessToken;
  final RefreshTokenReader _readRefreshToken;
  final TokenSaver _saveTokens;
  final SessionClearer _clearSession;

  @override
  Future<Map<String, dynamic>> getJson(
    String path, {
    Map<String, String>? queryParameters,
  }) {
    return _send(
      method: 'GET',
      path: path,
      queryParameters: queryParameters,
      authenticated: true,
    );
  }

  @override
  Future<Map<String, dynamic>> postJson(
    String path, {
    Object? body,
    Map<String, String>? queryParameters,
    bool authenticated = true,
  }) {
    return _send(
      method: 'POST',
      path: path,
      body: body,
      queryParameters: queryParameters,
      authenticated: authenticated,
    );
  }

  @override
  Future<void> deleteJson(String path) async {
    await _send(
      method: 'DELETE',
      path: path,
      authenticated: true,
      expectBody: false,
    );
  }

  Future<Map<String, dynamic>> _send({
    required String method,
    required String path,
    Object? body,
    Map<String, String>? queryParameters,
    required bool authenticated,
    bool expectBody = true,
    bool didRefresh = false,
  }) async {
    final uri = Uri.parse('$_baseUrl$path').replace(
      queryParameters: queryParameters?.isEmpty ?? true ? null : queryParameters,
    );

    final headers = <String, String>{
      'Accept': 'application/json',
      if (body != null) 'Content-Type': 'application/json',
    };

    if (authenticated) {
      final token = await _readAccessToken();
      if (token != null && token.isNotEmpty) {
        headers['Authorization'] = 'Bearer $token';
      }
    }

    late http.Response response;
    try {
      final encoded = body == null ? null : jsonEncode(body);
      response = switch (method) {
        'GET' => await _http.get(uri, headers: headers),
        'POST' => await _http.post(uri, headers: headers, body: encoded),
        'DELETE' => await _http.delete(uri, headers: headers),
        _ => throw AppException(message: 'Unsupported HTTP method: $method'),
      };
    } on AppException {
      rethrow;
    } catch (error) {
      throw AppException(
        message: 'Unable to reach the server. Check your connection.',
        code: 'NETWORK_ERROR',
        cause: error,
      );
    }

    if (response.statusCode == 401 && authenticated && !didRefresh) {
      final refreshed = await _tryRefresh();
      if (refreshed) {
        return _send(
          method: method,
          path: path,
          body: body,
          queryParameters: queryParameters,
          authenticated: authenticated,
          expectBody: expectBody,
          didRefresh: true,
        );
      }
    }

    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (!expectBody || response.body.isEmpty) {
        return <String, dynamic>{};
      }
      final decoded = jsonDecode(response.body);
      if (decoded is Map<String, dynamic>) {
        return decoded;
      }
      throw const AppException(
        message: 'Unexpected server response.',
        code: 'INVALID_RESPONSE',
      );
    }

    throw _mapError(response);
  }

  Future<bool> _tryRefresh() async {
    final refresh = await _readRefreshToken();
    if (refresh == null || refresh.isEmpty) {
      await _clearSession();
      return false;
    }

    try {
      final uri = Uri.parse('$_baseUrl/api/v1/auth/refresh');
      final response = await _http.post(
        uri,
        headers: {
          'Accept': 'application/json',
          'Content-Type': 'application/json',
        },
        body: jsonEncode({'refreshToken': refresh}),
      );

      if (response.statusCode < 200 || response.statusCode >= 300) {
        await _clearSession();
        return false;
      }

      final decoded = jsonDecode(response.body);
      if (decoded is! Map<String, dynamic>) {
        await _clearSession();
        return false;
      }

      final tokens = decoded['tokens'];
      if (tokens is! Map<String, dynamic>) {
        await _clearSession();
        return false;
      }

      final access = tokens['accessToken']?.toString();
      final nextRefresh = tokens['refreshToken']?.toString();
      if (access == null ||
          access.isEmpty ||
          nextRefresh == null ||
          nextRefresh.isEmpty) {
        await _clearSession();
        return false;
      }

      await _saveTokens(accessToken: access, refreshToken: nextRefresh);
      return true;
    } catch (_) {
      await _clearSession();
      return false;
    }
  }

  AppException _mapError(http.Response response) {
    String message = 'Request failed (${response.statusCode}).';
    String? code;

    if (response.body.isNotEmpty) {
      try {
        final decoded = jsonDecode(response.body);
        if (decoded is Map<String, dynamic>) {
          message = decoded['detail']?.toString() ??
              decoded['title']?.toString() ??
              message;
          code = decoded['code']?.toString();

          final errors = decoded['errors'];
          if (errors is Map && errors.isNotEmpty) {
            final first = errors.values.first;
            if (first is List && first.isNotEmpty) {
              message = first.first.toString();
            }
          }
        }
      } catch (_) {
        // Keep fallback message.
      }
    }

    return AppException(
      message: message,
      code: code ?? 'HTTP_${response.statusCode}',
    );
  }
}
