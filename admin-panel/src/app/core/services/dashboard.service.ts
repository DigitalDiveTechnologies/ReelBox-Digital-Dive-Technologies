import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  DashboardActivity,
  DashboardSummary,
  DashboardTrends,
} from '../api/models/admin-modules.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly api = inject(AdminApiClient);

  getSummary(): Observable<DashboardSummary> {
    return this.api.get<DashboardSummary>('/admin/dashboard/summary');
  }

  getTrends(days = 14): Observable<DashboardTrends> {
    return this.api.get<DashboardTrends>('/admin/dashboard/trends', { days });
  }

  getActivity(limit = 20): Observable<DashboardActivity> {
    return this.api.get<DashboardActivity>('/admin/dashboard/activity', {
      limit,
    });
  }
}
