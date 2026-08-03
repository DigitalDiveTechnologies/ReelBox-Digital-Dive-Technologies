import { Injectable, computed, signal } from '@angular/core';
import { AdminProfile } from '../models/admin-auth.models';
import { TokenService } from './token.service';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly _admin = signal<AdminProfile | null>(null);
  private readonly _restored = signal(false);

  readonly admin = this._admin.asReadonly();
  readonly restored = this._restored.asReadonly();

  readonly isAuthenticated = computed(
    () => this.tokens.hasAccessToken() && this._admin() !== null,
  );

  constructor(private readonly tokens: TokenService) {}

  /** True when a token exists (used by guards before profile hydrate finishes). */
  hasToken(): boolean {
    return this.tokens.hasAccessToken();
  }

  setAdmin(profile: AdminProfile): void {
    this._admin.set(profile);
  }

  clear(): void {
    this.tokens.clearTokens();
    this._admin.set(null);
  }

  markRestored(): void {
    this._restored.set(true);
  }

  hasRole(role: string): boolean {
    const roles = this._admin()?.roles ?? [];
    return roles.some((r) => r.toLowerCase() === role.toLowerCase());
  }

  hasPermission(permission: string): boolean {
    const permissions = this._admin()?.permissions ?? [];
    return permissions.some((p) => p.toLowerCase() === permission.toLowerCase());
  }

  hasAnyRole(roles: string[]): boolean {
    if (roles.length === 0) {
      return true;
    }
    return roles.some((role) => this.hasRole(role));
  }

  hasAnyPermission(permissions: string[]): boolean {
    if (permissions.length === 0) {
      return true;
    }
    return permissions.some((permission) => this.hasPermission(permission));
  }
}
