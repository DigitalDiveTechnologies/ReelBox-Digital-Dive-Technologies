import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AdminApiClient } from '../api/admin-api.client';
import {
  AdminMediaDetail,
  MediaListQuery,
  MediaPagedResult,
  PlaybackMetadata,
} from '../api/models/admin-phase6.models';

@Injectable({ providedIn: 'root' })
export class MediaAdminService {
  private readonly api = inject(AdminApiClient);

  list(query: MediaListQuery = {}): Observable<MediaPagedResult> {
    return this.api.get<MediaPagedResult>('/admin/media', {
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 25,
      search: query.search,
      status: query.status ?? undefined,
      platform: query.platform ?? undefined,
      userId: query.userId ?? undefined,
      sortBy: query.sortBy,
      sortDir: query.sortDir,
    });
  }

  get(id: string): Observable<AdminMediaDetail> {
    return this.api.get<AdminMediaDetail>(`/admin/media/${id}`);
  }

  delete(id: string): Observable<void> {
    return this.api.delete<void>(`/admin/media/${id}`);
  }

  retry(id: string): Observable<AdminMediaDetail> {
    return this.api.post<AdminMediaDetail>(`/admin/media/${id}/retry`, {});
  }

  playback(id: string): Observable<PlaybackMetadata> {
    return this.api.get<PlaybackMetadata>(`/admin/media/${id}/playback`);
  }
}
