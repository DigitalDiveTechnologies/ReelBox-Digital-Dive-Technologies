import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  DownloadsTrends,
  PlatformStats,
  ProviderPerformance,
  UserActivity,
} from '../api/models/admin-phase6.models';

export type ReportExportType = 'downloads' | 'users' | 'platforms';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly api = inject(AdminApiClient);

  downloadsTrends(days = 14): Observable<DownloadsTrends> {
    return this.api.get<DownloadsTrends>('/admin/reports/downloads-trends', {
      days,
    });
  }

  userActivity(days = 14): Observable<UserActivity> {
    return this.api.get<UserActivity>('/admin/reports/user-activity', { days });
  }

  platformStats(): Observable<PlatformStats> {
    return this.api.get<PlatformStats>('/admin/reports/platform-stats');
  }

  providerPerformance(): Observable<ProviderPerformance> {
    return this.api.get<ProviderPerformance>(
      '/admin/reports/provider-performance',
    );
  }

  downloadCsv(type: ReportExportType): Observable<Blob> {
    return this.api.getBlob('/admin/reports/export.csv', { type });
  }
}
