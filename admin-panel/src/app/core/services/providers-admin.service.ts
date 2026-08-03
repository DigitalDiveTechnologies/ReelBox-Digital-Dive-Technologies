import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  HealthComponentStatus,
  ProviderAdminItem,
  UpdateProviderRequest,
} from '../api/models/admin-phase6.models';

@Injectable({ providedIn: 'root' })
export class ProvidersAdminService {
  private readonly api = inject(AdminApiClient);

  list(): Observable<ProviderAdminItem[]> {
    return this.api.get<ProviderAdminItem[]>('/admin/providers');
  }

  update(
    name: string,
    body: UpdateProviderRequest,
  ): Observable<ProviderAdminItem> {
    return this.api.patch<ProviderAdminItem>(
      `/admin/providers/${encodeURIComponent(name)}`,
      body,
    );
  }

  healthCheck(name: string): Observable<HealthComponentStatus> {
    return this.api.post<HealthComponentStatus>(
      `/admin/providers/${encodeURIComponent(name)}/health-check`,
      {},
    );
  }
}
