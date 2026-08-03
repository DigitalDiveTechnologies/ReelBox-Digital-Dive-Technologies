export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages?: number;
}

export interface ListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}

export interface DashboardSummary {
  totalUsers: number;
  activeUsers: number;
  blockedUsers: number;
  totalMedia: number;
  completedMedia: number;
  failedMedia: number;
  downloadsToday: number;
  successRate: number;
  activeAdmins: number;
}

export interface DashboardTrendPoint {
  date: string;
  downloads: number;
  failures: number;
}

export interface DashboardTrends {
  items: DashboardTrendPoint[];
}

export interface DashboardActivityItem {
  id: string;
  type: string;
  title: string;
  createdAt: string;
}

export interface DashboardActivity {
  items: DashboardActivityItem[];
}

export interface MobileUserListItem {
  id: string;
  email: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  mediaCount: number;
}

export interface MobileUserDetail extends MobileUserListItem {
  hasActiveSession: boolean;
}

export interface UsersListQuery extends ListQuery {
  isActive?: boolean | null;
}

export interface AdminAccountListItem {
  id: string;
  email: string;
  displayName?: string | null;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAdminRequest {
  email: string;
  password: string;
  displayName?: string | null;
  role: string;
}

export interface UpdateAdminRequest {
  displayName?: string | null;
  role?: string | null;
  isActive?: boolean | null;
}

export interface AdminsListQuery extends ListQuery {
  role?: string | null;
  isActive?: boolean | null;
}

export interface RoleDefinition {
  name: string;
  description: string;
  permissions: string[];
}

export interface RolesListResponse {
  items: RoleDefinition[];
}

export interface AuditLogListItem {
  id: string;
  adminId: string;
  adminEmail: string;
  action: string;
  entityType: string;
  entityId?: string | null;
  createdAt: string;
  ipAddress?: string | null;
}

export interface AuditLogDetail extends AuditLogListItem {
  oldValuesJson?: string | null;
  newValuesJson?: string | null;
  correlationId?: string | null;
}

export interface AuditLogsListQuery extends ListQuery {
  adminId?: string | null;
  action?: string | null;
  fromUtc?: string | null;
  toUtc?: string | null;
}
