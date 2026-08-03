import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  OrphanScan,
  StorageCleanup,
  StorageCleanupRequest,
  StorageSummary,
} from '../api/models/admin-phase6.models';

@Injectable({ providedIn: 'root' })
export class StorageService {
  private readonly api = inject(AdminApiClient);

  summary(): Observable<StorageSummary> {
    return this.api.get<StorageSummary>('/admin/storage/summary');
  }

  orphanScan(): Observable<OrphanScan> {
    return this.api.post<OrphanScan>('/admin/storage/orphan-scan', {});
  }

  cleanup(body: StorageCleanupRequest): Observable<StorageCleanup> {
    return this.api.post<StorageCleanup>('/admin/storage/cleanup', body);
  }
}
