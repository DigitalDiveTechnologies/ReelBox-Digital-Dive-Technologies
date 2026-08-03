import { Injectable } from '@angular/core';

const ACCESS_KEY = 'reelbox_admin_access_token';
const REFRESH_KEY = 'reelbox_admin_refresh_token';
const REMEMBER_KEY = 'reelbox_admin_remember';

/**
 * Stores JWT access/refresh tokens.
 * SPA limitation: browser storage only (httpOnly cookies require backend cookie auth later).
 */
@Injectable({ providedIn: 'root' })
export class TokenService {
  getAccessToken(): string | null {
    return this.storage().getItem(ACCESS_KEY);
  }

  getRefreshToken(): string | null {
    return this.storage().getItem(REFRESH_KEY);
  }

  isRememberMe(): boolean {
    return localStorage.getItem(REMEMBER_KEY) === '1';
  }

  setRememberMe(remember: boolean): void {
    if (remember) {
      localStorage.setItem(REMEMBER_KEY, '1');
    } else {
      localStorage.removeItem(REMEMBER_KEY);
    }
  }

  saveTokens(accessToken: string, refreshToken: string, rememberMe: boolean): void {
    this.clearTokens();
    this.setRememberMe(rememberMe);
    const store = rememberMe ? localStorage : sessionStorage;
    store.setItem(ACCESS_KEY, accessToken);
    store.setItem(REFRESH_KEY, refreshToken);
  }

  clearTokens(): void {
    localStorage.removeItem(ACCESS_KEY);
    localStorage.removeItem(REFRESH_KEY);
    sessionStorage.removeItem(ACCESS_KEY);
    sessionStorage.removeItem(REFRESH_KEY);
  }

  hasAccessToken(): boolean {
    const token = this.getAccessToken();
    return !!token && token.trim().length > 0;
  }

  private storage(): Storage {
    return this.isRememberMe() ? localStorage : sessionStorage;
  }
}
