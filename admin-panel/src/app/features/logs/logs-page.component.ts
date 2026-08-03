import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import {
  AppErrorLogDetail,
  AppErrorLogListItem,
} from '../../core/api/models/admin-phase6.models';
import { LogsService } from '../../core/services/logs.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PaginationBarComponent } from '../../shared/components/pagination-bar/pagination-bar.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

@Component({
  selector: 'app-logs-page',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    PageHeaderComponent,
    PlaceholderCardComponent,
    EmptyStateComponent,
    PaginationBarComponent,
  ],
  template: `
    <app-page-header
      title="Error Logs"
      subtitle="Application error trail with correlation filters"
      [breadcrumbs]="breadcrumbs"
    />

    <app-placeholder-card title="Error Logs list" hint="Searchable operational log stream">
      <div class="toolbar">
        <input
          type="search"
          placeholder="Search message, source, path…"
          aria-label="Search error logs"
          [ngModel]="search()"
          (ngModelChange)="onSearch($event)"
        />
        <select
          aria-label="Filter by level"
          [ngModel]="level()"
          (ngModelChange)="onLevel($event)"
        >
          <option value="">All levels</option>
          <option value="Error">Error</option>
          <option value="Warning">Warning</option>
          <option value="Information">Information</option>
          <option value="Critical">Critical</option>
        </select>
        <input
          type="text"
          placeholder="Correlation ID"
          aria-label="Filter by correlation ID"
          [ngModel]="correlationId()"
          (ngModelChange)="onCorrelation($event)"
        />
        <input
          type="datetime-local"
          aria-label="From date"
          [ngModel]="fromLocal()"
          (ngModelChange)="onFrom($event)"
        />
        <input
          type="datetime-local"
          aria-label="To date"
          [ngModel]="toLocal()"
          (ngModelChange)="onTo($event)"
        />
      </div>

      @if (error()) {
        <div class="banner error" role="alert">{{ error() }}</div>
      }
      @if (loading()) {
        <div class="banner muted">Loading logs…</div>
      } @else if (!error() && items().length === 0) {
        <app-empty-state
          icon="✎"
          title="No error logs"
          message="Try clearing filters or wait for new application errors."
        />
      } @else if (!error()) {
        <div class="layout">
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>
                    <button type="button" class="sort" (click)="toggleSort('createdAt')">
                      Time {{ sortMark('createdAt') }}
                    </button>
                  </th>
                  <th>
                    <button type="button" class="sort" (click)="toggleSort('level')">
                      Level {{ sortMark('level') }}
                    </button>
                  </th>
                  <th>Message</th>
                  <th>Source</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                @for (row of items(); track row.id) {
                  <tr
                    [class.selected]="selectedId() === row.id"
                    (click)="selectRow(row.id)"
                  >
                    <td>{{ row.createdAt | date: 'short' }}</td>
                    <td>
                      <span class="badge" [attr.data-level]="row.level">{{ row.level }}</span>
                    </td>
                    <td class="msg">{{ row.message }}</td>
                    <td>{{ row.source || '—' }}</td>
                    <td>{{ row.statusCode ?? '—' }}</td>
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
              <h3>Log details</h3>
              <dl>
                <div><dt>Level</dt><dd>{{ detail()!.level }}</dd></div>
                <div><dt>Message</dt><dd>{{ detail()!.message }}</dd></div>
                <div><dt>Source</dt><dd>{{ detail()!.source || '—' }}</dd></div>
                <div><dt>Path</dt><dd>{{ detail()!.path || '—' }}</dd></div>
                <div><dt>Status</dt><dd>{{ detail()!.statusCode ?? '—' }}</dd></div>
                <div><dt>Correlation</dt><dd>{{ detail()!.correlationId || '—' }}</dd></div>
                <div><dt>When</dt><dd>{{ detail()!.createdAt | date: 'medium' }}</dd></div>
                <div>
                  <dt>Detail</dt>
                  <dd class="mono">{{ detail()!.detail || '—' }}</dd>
                </div>
              </dl>
            } @else {
              <app-empty-state
                icon="◇"
                title="Select a log"
                message="Choose a row to inspect the full detail text."
              />
            }
          </aside>
        </div>
      }
    </app-placeholder-card>
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
      select {
        border: 1px solid var(--border);
        background: var(--surface);
        color: var(--text-primary);
        border-radius: 10px;
        padding: 0.5rem 0.7rem;
        min-width: 150px;
        font: inherit;
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
        font-size: 0.86rem;
      }
      th,
      td {
        text-align: left;
        padding: 0.55rem 0.35rem;
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
      .msg {
        max-width: 280px;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
      .badge {
        display: inline-block;
        padding: 0.15rem 0.5rem;
        border-radius: 999px;
        font-size: 0.75rem;
        background: color-mix(in srgb, var(--status-queued) 25%, transparent);
      }
      .badge[data-level='Error'],
      .badge[data-level='Critical'] {
        background: color-mix(in srgb, var(--status-fail) 25%, transparent);
      }
      .badge[data-level='Warning'] {
        background: color-mix(in srgb, var(--brand) 25%, transparent);
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
        white-space: pre-wrap;
      }
      .muted {
        color: var(--text-muted);
      }
    `,
  ],
})
export class LogsPageComponent implements OnInit, OnDestroy {
  private readonly logs = inject(LogsService);
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();
  private readonly correlation$ = new Subject<string>();

  readonly breadcrumbs = [{ label: 'Error Logs' }];
  readonly loading = signal(false);
  readonly detailLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly items = signal<AppErrorLogListItem[]>([]);
  readonly detail = signal<AppErrorLogDetail | null>(null);
  readonly selectedId = signal<string | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly totalCount = signal(0);
  readonly search = signal('');
  readonly level = signal('');
  readonly correlationId = signal('');
  readonly fromLocal = signal('');
  readonly toLocal = signal('');
  readonly sortBy = signal('createdAt');
  readonly sortDir = signal<'asc' | 'desc'>('desc');

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((value) => {
        this.search.set(value);
        this.page.set(1);
        this.load();
      });
    this.correlation$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((value) => {
        this.correlationId.set(value);
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

  onCorrelation(value: string): void {
    this.correlation$.next(value);
  }

  onLevel(value: string): void {
    this.level.set(value);
    this.page.set(1);
    this.load();
  }

  onFrom(value: string): void {
    this.fromLocal.set(value);
    this.page.set(1);
    this.load();
  }

  onTo(value: string): void {
    this.toLocal.set(value);
    this.page.set(1);
    this.load();
  }

  toggleSort(column: string): void {
    if (this.sortBy() === column) {
      this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(column);
      this.sortDir.set('desc');
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
    this.detailLoading.set(true);
    this.logs.get(id).subscribe({
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

  private toUtc(local: string): string | undefined {
    if (!local) return undefined;
    const d = new Date(local);
    return Number.isNaN(d.getTime()) ? undefined : d.toISOString();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.logs
      .list({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search().trim() || undefined,
        level: this.level() || null,
        correlationId: this.correlationId().trim() || null,
        from: this.toUtc(this.fromLocal()) ?? null,
        to: this.toUtc(this.toLocal()) ?? null,
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
