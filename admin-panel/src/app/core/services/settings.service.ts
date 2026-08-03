import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  SettingsGrouped,
  UpsertSettingsRequest,
} from '../api/models/admin-phase6.models';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly api = inject(AdminApiClient);

  get(): Observable<SettingsGrouped> {
    return this.api.get<SettingsGrouped>('/admin/settings');
  }

  put(body: UpsertSettingsRequest): Observable<SettingsGrouped> {
    return this.api.put<SettingsGrouped>('/admin/settings', body);
  }
}
