import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import {
  AdminMediaDetail,
  AdminMediaListItem,
  PlaybackMetadata,
} from '../../core/api/models/admin-phase6.models';
import { SessionService } from '../../core/auth/session/session.service';
import { MediaAdminService } from '../../core/services/media-admin.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PaginationBarComponent } from '../../shared/components/pagination-bar/pagination-bar.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

type PendingAction = 'delete' | 'retry' | null;

const MANAGE_ROLES = ['SuperAdmin', 'Support', 'Operations'];

@Component({
  selector: 'app-media-page',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    FormsModule,
    PageHeaderComponent,
    PlaceholderCardComponent,
    EmptyStateComponent,
    PaginationBarComponent,
    ConfirmDialogComponent,
  ],
  template: `
    <app-page-header
      title="Media"
      subtitle="Saved media records — inspect, preview, retry, or delete"
      [breadcrumbs]="breadcrumbs"
    />

    <app-placeholder-card title="Media list" hint="Server-side search & pagination">
      <div class="toolbar">
        <input
          type="search"
          placeholder="Search URL, title, email…"
          aria-label="Search media"
          [ngModel]="search()"
          (ngModelChange)="onSearch($event)"
        />
        <select
          aria-label="Filter by status"
          [ngModel]="statusFilter()"
          (ngModelChange)="onStatusFilter($event)"
        >
          <option value="">All statuses</option>
          @for (s of statuses; track s) {
            <option [value]="s">{{ s }}</option>
          }
        </select>
        <select
          aria-label="Filter by platform"
          [ngModel]="platformFilter()"
          (ngModelChange)="onPlatformFilter($event)"
        >
          <option value="">All platforms</option>
          <option value="Instagram">Instagram</option>
          <option value="Facebook">Facebook</option>
        </select>
      </div>

      @if (error()) {
        <div class="banner error" role="alert">{{ error() }}</div>
      }
      @if (loading()) {
        <div class="banner muted">Loading media…</div>
      } @else if (!error() && items().length === 0) {
        <app-empty-state
          icon="▶"
          title="No media found"
          message="Try clearing search or filters."
        />
      } @else if (!error()) {
        <div class="layout">
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>
                    <button type="button" class="sort" (click)="toggleSort('platform')">
                      Platform {{ sortMark('platform') }}
                    </button>
                  </th>
                  <th>
                    <button type="button" class="sort" (click)="toggleSort('status')">
                      Status {{ sortMark('status') }}
                    </button>
                  </th>
                  <th>User</th>
                  <th>
                    <button type="button" class="sort" (click)="toggleSort('createdAt')">
                      Created {{ sortMark('createdAt') }}
                    </button>
                  </th>
                  <th>Size</th>
                </tr>
              </thead>
              <tbody>
                @for (row of items(); track row.id) {
                  <tr
                    [class.selected]="selectedId() === row.id"
                    (click)="selectRow(row.id)"
                  >
                    <td>{{ row.platform }}</td>
                    <td>
                      <span class="badge" [attr.data-status]="row.status">{{ row.status }}</span>
                    </td>
                    <td>{{ row.userEmail || row.userId }}</td>
                    <td>{{ row.createdAt | date: 'mediumDate' }}</td>
                    <td>
                      @if (row.fileSizeBytes != null) {
                        {{ row.fileSizeBytes / 1048576 | number: '1.0-2' }} MB
                      } @else {
                        —
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
            <app-pagination-bar
              [page]="page()"
              [pageSize]="pageSize()"
              [totalCount]="totalCount()"
              (pageChange)="goPage($event)"
              (pageSizeChange)="changePageSize($event)"
            />
          </div>

          <aside class="detail">
            @if (detailLoading()) {
              <p class="muted">Loading details…</p>
            } @else if (detail()) {
              <h3>Media details</h3>
              <dl>
                <div><dt>Title</dt><dd>{{ detail()!.title || '—' }}</dd></div>
                <div><dt>Status</dt><dd>{{ detail()!.status }}</dd></div>
                <div><dt>Platform</dt><dd>{{ detail()!.platform }}</dd></div>
                <div><dt>User</dt><dd>{{ detail()!.userEmail || detail()!.userId }}</dd></div>
                <div><dt>URL</dt><dd class="mono">{{ detail()!.originalUrl }}</dd></div>
                <div><dt>MIME</dt><dd>{{ detail()!.mimeType || '—' }}</dd></div>
                <div><dt>Retries</dt><dd>{{ detail()!.retryCount }}</dd></div>
                <div><dt>Error</dt><dd>{{ detail()!.errorCode || '—' }}</dd></div>
                @if (detail()!.errorMessage) {
                  <div><dt>Message</dt><dd>{{ detail()!.errorMessage }}</dd></div>
                }
                <div><dt>Created</dt><dd>{{ detail()!.createdAt | date: 'medium' }}</dd></div>
                <div><dt>Updated</dt><dd>{{ detail()!.updatedAt | date: 'medium' }}</dd></div>
              </dl>

              @if (playback()) {
                <div class="preview">
                  <h4>Preview</h4>
                  @if (playback()!.playbackUrl && isVideo(playback()!.mimeType)) {
                    <video controls [src]="playback()!.playbackUrl!"></video>
                  } @else if (playback()!.playbackUrl) {
                    <a [href]="playback()!.playbackUrl!" target="_blank" rel="noopener">
                      Open playback URL
                    </a>
                  } @else {
                    <p class="muted">No playback URL ({{ playback()!.status }}).</p>
                  }
                </div>
              }

              @if (canManage()) {
                <div class="actions">
                  <button type="button" (click)="loadPlayback()">Preview</button>
                  <button type="button" (click)="ask('retry')">Retry</button>
                  <button type="button" class="warn" (click)="ask('delete')">Delete</button>
                </div>
              }
            } @else {
              <app-empty-state
                icon="◇"
                title="Select media"
                message="Choose a row to view details and actions."
              />
            }
          </aside>
        </div>
      }
    </app-placeholder-card>

    <app-confirm-dialog
      [open]="pending() !== null"
      [title]="confirmTitle"
      [message]="confirmMessage"
      [confirmLabel]="confirmLabel"
      [busy]="actionBusy()"
      (confirm)="runPending()"
      (cancel)="pending.set(null)"
    />
  `,
  styles: [
    `
      .toolbar {
        display: flex;
        flex-wrap: wrap;
        gap: 0.65rem;
        margin-bottom: 1rem;
      }
      input,
      select,
      button {
        font: inherit;
      }
      input,
      select {
        border: 1px solid var(--border);
        background: var(--surface);
        color: var(--text-primary);
        border-radius: 10px;
        padding: 0.5rem 0.7rem;
        min-width: 160px;
      }
      .banner {
        margin-bottom: 0.85rem;
        padding: 0.7rem 0.85rem;
        border-radius: 12px;
        font-size: 0.86rem;
      }
      .banner.error {
        border: 1px solid color-mix(in srgb, var(--status-fail) 45%, transparent);
        background: color-mix(in srgb, var(--status-fail) 14%, transparent);
        color: #fecaca;
      }
      .banner.muted {
        border: 1px solid var(--border);
        color: var(--text-muted);
      }
      .layout {
        display: grid;
        grid-template-columns: minmax(0, 1.6fr) minmax(260px, 1fr);
        gap: 1rem;
      }
      @media (max-width: 960px) {
        .layout {
          grid-template-columns: 1fr;
        }
      }
      .table-wrap {
        overflow-x: auto;
      }
      table {
        width: 100%;
        border-collapse: collapse;
        font-size: 0.88rem;
      }
      th,
      td {
        text-align: left;
        padding: 0.6rem 0.4rem;
        border-bottom: 1px solid color-mix(in srgb, var(--border) 55%, transparent);
      }
      th {
        color: var(--text-muted);
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      tbody tr {
        cursor: pointer;
      }
      tbody tr:hover,
      tbody tr.selected {
        background: color-mix(in srgb, var(--brand) 10%, transparent);
      }
      .sort {
        background: none;
        border: none;
        color: inherit;
        padding: 0;
        cursor: pointer;
        font: inherit;
        text-transform: inherit;
        letter-spacing: inherit;
      }
      .badge {
        display: inline-block;
        padding: 0.15rem 0.5rem;
        border-radius: 999px;
        background: color-mix(in srgb, var(--status-queued) 25%, transparent);
        font-size: 0.75rem;
      }
      .badge[data-status='Completed'] {
        background: color-mix(in srgb, var(--status-ok) 25%, transparent);
      }
      .badge[data-status='Failed'] {
        background: color-mix(in srgb, var(--status-fail) 25%, transparent);
      }
      .detail {
        border: 1px solid color-mix(in srgb, var(--border) 60%, transparent);
        border-radius: var(--radius);
        padding: 1rem;
        background: var(--surface);
      }
      .detail h3 {
        margin: 0 0 0.85rem;
        font-size: 0.95rem;
      }
      .detail h4 {
        margin: 1rem 0 0.5rem;
        font-size: 0.85rem;
      }
      dl {
        margin: 0;
        display: grid;
        gap: 0.55rem;
      }
      dl div {
        display: grid;
        gap: 0.15rem;
      }
      dt {
        font-size: 0.72rem;
        color: var(--text-muted);
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      dd {
        margin: 0;
        font-size: 0.9rem;
        word-break: break-word;
      }
      .mono {
        font-family: ui-monospace, monospace;
        font-size: 0.78rem;
      }
      .preview video {
        width: 100%;
        max-height: 220px;
        border-radius: 10px;
        background: #000;
      }
      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        margin-top: 1rem;
      }
      .actions button {
        border: 1px solid var(--border);
        background: transparent;
        color: var(--text-primary);
        border-radius: 10px;
        padding: 0.45rem 0.75rem;
        cursor: pointer;
      }
      .actions .warn {
        border-color: color-mix(in srgb, var(--status-fail) 50%, transparent);
        color: #fecaca;
      }
      .muted {
        color: var(--text-muted);
      }
    `,
  ],
})
export class MediaPageComponent implements OnInit, OnDestroy {
  private readonly media = inject(MediaAdminService);
  private readonly session = inject(SessionService);
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();

  readonly breadcrumbs = [{ label: 'Media' }];
  readonly statuses = [
    'Preparing',
    'Queued',
    'Downloading',
    'Processing',
    'Completed',
    'Failed',
  ];
  readonly loading = signal(false);
  readonly detailLoading = signal(false);
  readonly actionBusy = signal(false);
  readonly error = signal<string | null>(null);
  readonly items = signal<AdminMediaListItem[]>([]);
  readonly detail = signal<AdminMediaDetail | null>(null);
  readonly playback = signal<PlaybackMetadata | null>(null);
  readonly selectedId = signal<string | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly totalCount = signal(0);
  readonly search = signal('');
  readonly statusFilter = signal('');
  readonly platformFilter = signal('');
  readonly sortBy = signal('createdAt');
  readonly sortDir = signal<'asc' | 'desc'>('desc');
  readonly pending = signal<PendingAction>(null);

  canManage(): boolean {
    return this.session.hasAnyRole(MANAGE_ROLES);
  }

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((value) => {
        this.search.set(value);
        this.page.set(1);
        this.load();
      });
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearch(value: string): void {
    this.search$.next(value);
  }

  onStatusFilter(value: string): void {
    this.statusFilter.set(value);
    this.page.set(1);
    this.load();
  }

  onPlatformFilter(value: string): void {
    this.platformFilter.set(value);
    this.page.set(1);
    this.load();
  }

  toggleSort(column: string): void {
    if (this.sortBy() === column) {
      this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(column);
      this.sortDir.set(column === 'platform' ? 'asc' : 'desc');
    }
    this.load();
  }

  sortMark(column: string): string {
    if (this.sortBy() !== column) return '';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  goPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  changePageSize(size: number): void {
    this.pageSize.set(size);
    this.page.set(1);
    this.load();
  }

  selectRow(id: string): void {
    this.selectedId.set(id);
    this.playback.set(null);
    this.detailLoading.set(true);
    this.media.get(id).subscribe({
      next: (detail) => {
        this.detail.set(detail);
        this.detailLoading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.detailLoading.set(false);
      },
    });
  }

  loadPlayback(): void {
    const id = this.selectedId();
    if (!id) return;
    this.media.playback(id).subscribe({
      next: (meta) => this.playback.set(meta),
      error: (err: Error) => this.error.set(err.message),
    });
  }

  isVideo(mime?: string | null): boolean {
    return !!mime && mime.startsWith('video/');
  }

  ask(action: PendingAction): void {
    this.pending.set(action);
  }

  get confirmTitle(): string {
    return this.pending() === 'delete' ? 'Delete media' : 'Retry media';
  }

  get confirmMessage(): string {
    return this.pending() === 'delete'
      ? 'This permanently deletes the media record and stored objects.'
      : 'Re-queue this media item for download?';
  }

  get confirmLabel(): string {
    return this.pending() === 'delete' ? 'Delete' : 'Retry';
  }

  runPending(): void {
    const id = this.selectedId();
    const action = this.pending();
    if (!id || !action) return;
    this.actionBusy.set(true);
    const done = () => {
      this.actionBusy.set(false);
      this.pending.set(null);
      if (action === 'delete') {
        this.detail.set(null);
        this.selectedId.set(null);
        this.playback.set(null);
        this.load();
        return;
      }
      this.load();
      this.selectRow(id);
    };
    const fail = (err: Error) => {
      this.actionBusy.set(false);
      this.pending.set(null);
      this.error.set(err.message);
    };

    if (action === 'delete') {
      this.media.delete(id).subscribe({ next: done, error: fail });
      return;
    }
    this.media.retry(id).subscribe({ next: done, error: fail });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.media
      .list({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search().trim() || undefined,
        status: this.statusFilter() || null,
        platform: this.platformFilter() || null,
        sortBy: this.sortBy(),
        sortDir: this.sortDir(),
      })
      .subscribe({
        next: (result) => {
          this.items.set(result.items ?? []);
          this.totalCount.set(result.totalCount ?? 0);
          this.loading.set(false);
        },
        error: (err: Error) => {
          this.error.set(err.message);
          this.loading.set(false);
        },
      });
  }
}
