import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  PlatformAdminItem,
  UpdatePlatformRequest,
} from '../api/models/admin-phase6.models';

@Injectable({ providedIn: 'root' })
export class PlatformsService {
  private readonly api = inject(AdminApiClient);

  list(): Observable<PlatformAdminItem[]> {
    return this.api.get<PlatformAdminItem[]>('/admin/platforms');
  }

  update(
    platform: string,
    body: UpdatePlatformRequest,
  ): Observable<PlatformAdminItem> {
    return this.api.patch<PlatformAdminItem>(
      `/admin/platforms/${encodeURIComponent(platform)}`,
      body,
    );
  }
}
