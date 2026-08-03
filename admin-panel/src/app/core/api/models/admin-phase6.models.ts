import { ListQuery, PagedResult } from './admin-modules.models';

export interface AdminMediaListItem {
  id: string;
  userId: string;
  userEmail?: string | null;
  platform: string;
  status: string;
  originalUrl: string;
  title?: string | null;
  fileSizeBytes?: number | null;
  retryCount: number;
  createdAt: string;
  updatedAt: string;
  errorCode?: string | null;
}

export interface AdminMediaDetail extends AdminMediaListItem {
  normalizedUrl?: string | null;
  thumbnailStorageKey?: string | null;
  mediaStorageKey?: string | null;
  mimeType?: string | null;
  durationMs?: number | null;
  progressPercent?: number | null;
  downloadStartedAt?: string | null;
  downloadedAt?: string | null;
  nextRetryAt?: string | null;
  errorMessage?: string | null;
}

export interface PlaybackMetadata {
  mediaId: string;
  userId?: string;
  status: string;
  mediaStorageKey?: string | null;
  thumbnailStorageKey?: string | null;
  mimeType?: string | null;
  playbackUrl?: string | null;
  thumbnailUrl?: string | null;
  delivery?: string;
  expiresAt?: string | null;
}

export interface MediaListQuery extends ListQuery {
  status?: string | null;
  platform?: string | null;
  userId?: string | null;
}

export interface JobStatusCounts {
  queued: number;
  active: number;
  completed: number;
  failed: number;
  total: number;
}

export interface JobsListResponse {
  items: AdminMediaListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages?: number;
  counts: JobStatusCounts;
}

export interface JobsListQuery extends ListQuery {
  statusGroup?: 'all' | 'queued' | 'active' | 'completed' | 'failed';
  platform?: string | null;
  userId?: string | null;
}

export interface PlatformAdminItem {
  platform: string;
  enabled: boolean;
  maintenanceMode: boolean;
  dailyLimit: number;
  status: string;
}

export interface UpdatePlatformRequest {
  enabled?: boolean | null;
  dailyLimit?: number | null;
  maintenanceMode?: boolean | null;
}

export interface ProviderAdminItem {
  name: string;
  platform: string;
  enabled: boolean;
  timeoutSeconds: number;
  priority: number;
  resolver: string;
  health: string;
  hasAccessToken: boolean;
  hasRapidApiKey: boolean;
  retryEligible?: boolean;
}

export interface UpdateProviderRequest {
  timeoutSeconds?: number | null;
  priority?: number | null;
  enabled?: boolean | null;
}

export interface HealthComponentStatus {
  name: string;
  status: string;
  detail?: string | null;
}

export interface StorageSummary {
  providerName: string;
  mediaCount: number;
  totalBytes: number;
  orphanEstimate?: number | null;
}

export interface OrphanScan {
  supported: boolean;
  message?: string | null;
  orphanKeys: string[];
  fileCount: number;
  dbKeyCount: number;
}

export interface StorageCleanup {
  supported: boolean;
  message?: string | null;
  deletedCount: number;
  deletedKeys: string[];
}

export interface StorageCleanupRequest {
  keys: string[];
}

export interface DownloadsTrendPoint {
  date: string;
  downloads: number;
  failures: number;
}

export interface DownloadsTrends {
  items: DownloadsTrendPoint[];
}

export interface UserActivityPoint {
  date: string;
  newUsers: number;
  downloads: number;
}

export interface UserActivity {
  items: UserActivityPoint[];
}

export interface PlatformStatItem {
  platform: string;
  total: number;
  completed: number;
  failed: number;
  successRate: number;
}

export interface PlatformStats {
  items: PlatformStatItem[];
}

export interface ProviderPerformanceItem {
  platform: string;
  success: number;
  fail: number;
  successRate: number;
}

export interface ProviderPerformance {
  items: ProviderPerformanceItem[];
}

export interface SystemHealthOverview {
  overallStatus: string;
  components: HealthComponentStatus[];
}

export interface AppErrorLogListItem {
  id: string;
  level: string;
  message: string;
  source?: string | null;
  correlationId?: string | null;
  path?: string | null;
  statusCode?: number | null;
  createdAt: string;
}

export interface AppErrorLogDetail extends AppErrorLogListItem {
  detail?: string | null;
}

export interface LogsListQuery extends ListQuery {
  level?: string | null;
  correlationId?: string | null;
  from?: string | null;
  to?: string | null;
}

export interface SettingItem {
  key: string;
  value: string;
  category: string;
}

export interface SettingsGrouped {
  groups: Record<string, SettingItem[]>;
}

export interface UpsertSettingsRequest {
  settings: Record<string, string>;
}

export type MediaPagedResult = PagedResult<AdminMediaListItem>;
export type LogsPagedResult = PagedResult<AppErrorLogListItem>;
