/** Typed contracts for Admin Auth APIs. */

export interface AdminLoginRequest {
  email: string;
  password: string;
}

export interface AdminForgotPasswordRequest {
  email: string;
}

export interface AdminResetPasswordRequest {
  email: string;
  otp: string;
  newPassword: string;
}

export interface AdminMessageResponse {
  message: string;
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

export const AdminAuthEndpoints = {
  login: '/admin/auth/login',
  refresh: '/admin/auth/refresh',
  logout: '/admin/auth/logout',
  me: '/admin/auth/me',
  forgotPassword: '/admin/auth/forgot-password',
  resetPassword: '/admin/auth/reset-password',
} as const;
