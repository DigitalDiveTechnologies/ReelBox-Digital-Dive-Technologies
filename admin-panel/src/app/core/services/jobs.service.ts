import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  AdminMediaDetail,
  JobsListQuery,
  JobsListResponse,
} from '../api/models/admin-phase6.models';

@Injectable({ providedIn: 'root' })
export class JobsService {
  private readonly api = inject(AdminApiClient);

  list(query: JobsListQuery = {}): Observable<JobsListResponse> {
    return this.api.get<JobsListResponse>('/admin/jobs', {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 25,
      statusGroup: query.statusGroup ?? 'all',
      search: query.search,
      platform: query.platform ?? undefined,
      userId: query.userId ?? undefined,
      sortBy: query.sortBy,
      sortDir: query.sortDir,
    });
  }

  retry(id: string): Observable<AdminMediaDetail> {
    return this.api.post<AdminMediaDetail>(`/admin/jobs/${id}/retry`, {});
  }

  cancel(id: string): Observable<void> {
    return this.api.post<void>(`/admin/jobs/${id}/cancel`, {});
  }

  requeue(id: string): Observable<void> {
    return this.api.post<void>(`/admin/jobs/${id}/requeue`, {});
  }
}
