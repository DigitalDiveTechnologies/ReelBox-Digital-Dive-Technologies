import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  MobileUserDetail,
  MobileUserListItem,
  PagedResult,
  UsersListQuery,
} from '../api/models/admin-modules.models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly api = inject(AdminApiClient);

  list(query: UsersListQuery = {}): Observable<PagedResult<MobileUserListItem>> {
    return this.api.get<PagedResult<MobileUserListItem>>('/admin/users', {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 25,
      search: query.search,
      isActive: query.isActive ?? undefined,
      sortBy: query.sortBy,
      sortDir: query.sortDir,
    });
  }

  get(id: string): Observable<MobileUserDetail> {
    return this.api.get<MobileUserDetail>(`/admin/users/${id}`);
  }

  updateStatus(id: string, isActive: boolean): Observable<void> {
    return this.api.patch<void>(`/admin/users/${id}/status`, { isActive });
  }

  revokeSessions(id: string): Observable<void> {
    return this.api.post<void>(`/admin/users/${id}/revoke-sessions`, {});
  }
}
