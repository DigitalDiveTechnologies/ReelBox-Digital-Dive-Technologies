import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  AuditLogDetail,
  AuditLogListItem,
  AuditLogsListQuery,
  PagedResult,
} from '../api/models/admin-modules.models';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private readonly api = inject(AdminApiClient);

  list(
    query: AuditLogsListQuery = {},
  ): Observable<PagedResult<AuditLogListItem>> {
    return this.api.get<PagedResult<AuditLogListItem>>('/admin/audit-logs', {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 25,
      search: query.search,
      adminId: query.adminId ?? undefined,
      action: query.action ?? undefined,
      fromUtc: query.fromUtc ?? undefined,
      toUtc: query.toUtc ?? undefined,
      sortBy: query.sortBy,
      sortDir: query.sortDir,
    });
  }

  get(id: string): Observable<AuditLogDetail> {
    return this.api.get<AuditLogDetail>(`/admin/audit-logs/${id}`);
  }
}
