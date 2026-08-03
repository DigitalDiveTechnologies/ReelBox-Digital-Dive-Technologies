import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import { SystemHealthOverview } from '../api/models/admin-phase6.models';

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly api = inject(AdminApiClient);

  overview(): Observable<SystemHealthOverview> {
    return this.api.get<SystemHealthOverview>('/admin/health/overview');
  }
}
