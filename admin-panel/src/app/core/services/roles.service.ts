import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  AdminAccountListItem,
  RolesListResponse,
} from '../api/models/admin-modules.models';

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly api = inject(AdminApiClient);

  list(): Observable<RolesListResponse> {
    return this.api.get<RolesListResponse>('/admin/roles');
  }

  assign(adminId: string, role: string): Observable<AdminAccountListItem> {
    return this.api.patch<AdminAccountListItem>(
      `/admin/roles/admins/${adminId}`,
      { role },
    );
  }
}
