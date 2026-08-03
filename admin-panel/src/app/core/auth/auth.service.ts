import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Observable,
  catchError,
  finalize,
  map,
  of,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { publicRuntimeConfig } from '../config/public-runtime.config';
import {
  AdminAuthEndpoints,
  AdminAuthResponse,
  AdminLoginRequest,
  AdminProfile,
  AdminRefreshRequest,
} from './models/admin-auth.models';
import { SessionService } from './session/session.service';
import { TokenService } from './session/token.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokens = inject(TokenService);
  private readonly session = inject(SessionService);

  login(
    request: AdminLoginRequest,
    rememberMe: boolean,
  ): Observable<AdminProfile> {
    return this.postAuth<AdminAuthResponse>(AdminAuthEndpoints.login, request).pipe(
      tap((response) => this.applyAuthResponse(response, rememberMe)),
      map((response) => response.admin),
      catchError((error: unknown) => throwError(() => this.toAuthError(error))),
    );
  }

  logout(): Observable<void> {
    const refreshToken = this.tokens.getRefreshToken();
    const call$ =
      this.canCallApi() && refreshToken
        ? this.postAuth<void>(AdminAuthEndpoints.logout, { refreshToken }).pipe(
            catchError(() => of(void 0)),
          )
        : of(void 0);

    return call$.pipe(
      finalize(() => this.session.clear()),
      map(() => void 0),
    );
  }

  refresh(): Observable<AdminAuthResponse> {
    const refreshToken = this.tokens.getRefreshToken();
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available.'));
    }

    const body: AdminRefreshRequest = { refreshToken };
    return this.postAuth<AdminAuthResponse>(AdminAuthEndpoints.refresh, body).pipe(
      tap((response) =>
        this.applyAuthResponse(response, this.tokens.isRememberMe()),
      ),
      catchError((error: unknown) => {
        this.session.clear();
        return throwError(() => this.toAuthError(error));
      }),
    );
  }

  loadCurrentAdmin(): Observable<AdminProfile> {
    return this.getAuth<AdminProfile>(AdminAuthEndpoints.me).pipe(
      tap((profile) => this.session.setAdmin(profile)),
      catchError((error: unknown) => throwError(() => this.toAuthError(error))),
    );
  }

  /**
   * Restore JWT session after browser refresh.
   * TODO(backend): `/admin/auth/me` must exist for full hydrate; until then restore fails closed.
   */
  restoreSession(): Observable<boolean> {
    if (!this.tokens.hasAccessToken()) {
      this.session.markRestored();
      return of(false);
    }

    if (!this.canCallApi()) {
      this.session.clear();
      this.session.markRestored();
      return of(false);
    }

    return this.loadCurrentAdmin().pipe(
      map(() => true),
      catchError(() =>
        this.refresh().pipe(
          switchMap(() => this.loadCurrentAdmin()),
          map(() => true),
          catchError(() => {
            this.session.clear();
            return of(false);
          }),
        ),
      ),
      finalize(() => this.session.markRestored()),
    );
  }

  private applyAuthResponse(response: AdminAuthResponse, rememberMe: boolean): void {
    this.tokens.saveTokens(
      response.tokens.accessToken,
      response.tokens.refreshToken,
      rememberMe,
    );
    this.session.setAdmin(response.admin);
  }

  private postAuth<T>(path: string, body: unknown): Observable<T> {
    if (!this.canCallApi()) {
      return throwError(
        () =>
          new Error(
            'Admin API base URL is not configured. Set publicRuntimeConfig.apiBaseUrl when Admin Auth endpoints are available.',
          ),
      );
    }
    return this.http.post<T>(this.url(path), body);
  }

  private getAuth<T>(path: string): Observable<T> {
    if (!this.canCallApi()) {
      return throwError(
        () =>
          new Error(
            'Admin API base URL is not configured. Set publicRuntimeConfig.apiBaseUrl when Admin Auth endpoints are available.',
          ),
      );
    }
    return this.http.get<T>(this.url(path));
  }

  private url(path: string): string {
    const base = publicRuntimeConfig.apiBaseUrl.replace(/\/$/, '');
    return `${base}${path.startsWith('/') ? path : `/${path}`}`;
  }

  private canCallApi(): boolean {
    return publicRuntimeConfig.apiBaseUrl.trim().length > 0;
  }

  private toAuthError(error: unknown): Error {
    if (error instanceof Error && !(error instanceof HttpErrorResponse)) {
      return error;
    }

    if (error instanceof HttpErrorResponse) {
      if (error.status === 0) {
        return new Error(
          'Cannot reach Admin API. Confirm apiBaseUrl and that Admin Auth endpoints are deployed.',
        );
      }
      if (error.status === 401) {
        return new Error('Invalid email or password.');
      }
      if (error.status === 403) {
        return new Error('You do not have permission to access the admin portal.');
      }
      if (error.status === 404) {
        return new Error(
          'Admin Auth endpoints are not available yet (TODO: backend /api/admin/auth/*).',
        );
      }

      const problem = error.error as { title?: string; detail?: string; message?: string } | null;
      const message =
        problem?.detail ?? problem?.title ?? problem?.message ?? error.message;
      return new Error(message || 'Authentication failed.');
    }

    return new Error('Authentication failed.');
  }
}
