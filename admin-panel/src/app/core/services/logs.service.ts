import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  AppErrorLogDetail,
  LogsListQuery,
  LogsPagedResult,
} from '../api/models/admin-phase6.models';

@Injectable({ providedIn: 'root' })
export class LogsService {
  private readonly api = inject(AdminApiClient);

  list(query: LogsListQuery = {}): Observable<LogsPagedResult> {
    return this.api.get<LogsPagedResult>('/admin/logs', {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 25,
      search: query.search,
      level: query.level ?? undefined,
      correlationId: query.correlationId ?? undefined,
      from: query.from ?? undefined,
      to: query.to ?? undefined,
      sortBy: query.sortBy,
      sortDir: query.sortDir,
    });
  }

  get(id: string): Observable<AppErrorLogDetail> {
    return this.api.get<AppErrorLogDetail>(`/admin/logs/${id}`);
  }
}
