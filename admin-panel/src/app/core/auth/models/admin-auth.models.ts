/** Typed contracts for PDF §9.2 Admin Auth APIs. Backend not implemented yet. */

export interface AdminLoginRequest {
  email: string;
  password: string;
}

export interface AdminTokens {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds?: number;
}

export interface AdminProfile {
  id: string;
  email: string;
  displayName?: string;
  roles: string[];
  permissions: string[];
}

export interface AdminAuthResponse {
  admin: AdminProfile;
  tokens: AdminTokens;
}

export interface AdminRefreshRequest {
  refreshToken: string;
}

/**
 * Expected Admin Auth routes (PDF).
 * TODO(backend): implement these under ASP.NET `/api/admin` without changing mobile `/api/v1/auth`.
 */
export const AdminAuthEndpoints = {
  login: '/admin/auth/login',
  refresh: '/admin/auth/refresh',
  logout: '/admin/auth/logout',
  me: '/admin/auth/me',
} as const;
