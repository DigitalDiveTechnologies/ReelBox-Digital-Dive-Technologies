import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  AdminAccountListItem,
  AdminsListQuery,
  CreateAdminRequest,
  PagedResult,
  UpdateAdminRequest,
} from '../api/models/admin-modules.models';

@Injectable({ providedIn: 'root' })
export class AdminsService {
  private readonly api = inject(AdminApiClient);

  list(
    query: AdminsListQuery = {},
  ): Observable<PagedResult<AdminAccountListItem>> {
    return this.api.get<PagedResult<AdminAccountListItem>>('/admin/admins', {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 25,
      search: query.search,
      role: query.role ?? undefined,
      isActive: query.isActive ?? undefined,
      sortBy: query.sortBy,
      sortDir: query.sortDir,
    });
  }

  get(id: string): Observable<AdminAccountListItem> {
    return this.api.get<AdminAccountListItem>(`/admin/admins/${id}`);
  }

  create(request: CreateAdminRequest): Observable<AdminAccountListItem> {
    return this.api.post<AdminAccountListItem>('/admin/admins', request);
  }

  update(
    id: string,
    request: UpdateAdminRequest,
  ): Observable<AdminAccountListItem> {
    return this.api.patch<AdminAccountListItem>(
      `/admin/admins/${id}`,
      request,
    );
  }
}
