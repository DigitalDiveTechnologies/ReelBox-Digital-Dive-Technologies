import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import {
  AuditLogDetail,
  AuditLogListItem,
} from '../../core/api/models/admin-modules.models';
import { AuditService } from '../../core/services/audit.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PaginationBarComponent } from '../../shared/components/pagination-bar/pagination-bar.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

@Component({
  selector: 'app-audit-page',
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
      title="Audit Logs"
      subtitle="Privileged admin actions with searchable history"
      [breadcrumbs]="breadcrumbs"
    />

    <app-placeholder-card title="Audit Logs list" hint="Immutable event trail">
      <div class="toolbar">
        <input
          type="search"
          placeholder="Search admin, action, entity…"
          aria-label="Search audit logs"
          [ngModel]="search()"
          (ngModelChange)="onSearch($event)"
        />
        <input
          type="text"
          placeholder="Action filter (exact)"
          [ngModel]="action()"
          (ngModelChange)="onAction($event)"
        />
        <input
          type="datetime-local"
          [ngModel]="fromLocal()"
          (ngModelChange)="onFrom($event)"
        />
        <input
          type="datetime-local"
          [ngModel]="toLocal()"
          (ngModelChange)="onTo($event)"
        />
      </div>

      @if (error()) {
        <div class="banner error" role="alert">{{ error() }}</div>
      }
      @if (loading()) {
        <div class="banner muted">Loading audit logs…</div>
      } @else if (!error() && items().length === 0) {
        <app-empty-state
          icon="✎"
          title="No audit entries"
          message="Privileged actions will appear here after admins make changes."
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
                    <button type="button" class="sort" (click)="toggleSort('adminEmail')">
                      Admin {{ sortMark('adminEmail') }}
                    </button>
                  </th>
                  <th>
                    <button type="button" class="sort" (click)="toggleSort('action')">
                      Action {{ sortMark('action') }}
                    </button>
                  </th>
                  <th>
                    <button type="button" class="sort" (click)="toggleSort('entityType')">
                      Entity {{ sortMark('entityType') }}
                    </button>
                  </th>
                  <th>IP</th>
                </tr>
              </thead>
              <tbody>
                @for (item of items(); track item.id) {
                  <tr
                    [class.selected]="selectedId() === item.id"
                    (click)="select(item.id)"
                  >
                    <td>{{ item.createdAt | date: 'medium' }}</td>
                    <td>{{ item.adminEmail }}</td>
                    <td><code>{{ item.action }}</code></td>
                    <td>{{ item.entityType }}{{ item.entityId ? ' · ' + shortId(item.entityId) : '' }}</td>
                    <td>{{ item.ipAddress || '—' }}</td>
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
              <h3>Audit details</h3>
              <dl>
                <div><dt>Time</dt><dd>{{ detail()!.createdAt | date: 'medium' }}</dd></div>
                <div><dt>Admin</dt><dd>{{ detail()!.adminEmail }}</dd></div>
                <div><dt>Action</dt><dd><code>{{ detail()!.action }}</code></dd></div>
                <div>
                  <dt>Entity</dt>
                  <dd>{{ detail()!.entityType }} {{ detail()!.entityId || '' }}</dd>
                </div>
                <div><dt>IP</dt><dd>{{ detail()!.ipAddress || '—' }}</dd></div>
                <div><dt>Correlation</dt><dd>{{ detail()!.correlationId || '—' }}</dd></div>
              </dl>
              <h4>Old values</h4>
              <pre>{{ pretty(detail()!.oldValuesJson) }}</pre>
              <h4>New values</h4>
              <pre>{{ pretty(detail()!.newValuesJson) }}</pre>
            } @else {
              <app-empty-state
                icon="◇"
                title="Select an entry"
                message="Choose a row to inspect payload details."
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
      button {
        font: inherit;
      }
      input {
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
        grid-template-columns: minmax(0, 1.5fr) minmax(280px, 1fr);
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
        font-size: 0.85rem;
      }
      th,
      td {
        text-align: left;
        padding: 0.55rem 0.35rem;
        border-bottom: 1px solid color-mix(in srgb, var(--border) 55%, transparent);
        vertical-align: top;
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
      code {
        font-size: 0.78rem;
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
        margin: 1rem 0 0.35rem;
        font-size: 0.78rem;
        color: var(--text-muted);
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      dl {
        margin: 0;
        display: grid;
        gap: 0.5rem;
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
        font-size: 0.88rem;
        word-break: break-word;
      }
      pre {
        margin: 0;
        padding: 0.65rem;
        border-radius: 10px;
        background: var(--bg-deep);
        border: 1px solid var(--border);
        font-size: 0.75rem;
        overflow: auto;
        max-height: 180px;
        white-space: pre-wrap;
        word-break: break-word;
      }
      .muted {
        color: var(--text-muted);
      }
    `,
  ],
})
export class AuditPageComponent implements OnInit, OnDestroy {
  private readonly audit = inject(AuditService);
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();
  private readonly action$ = new Subject<string>();

  readonly breadcrumbs = [{ label: 'Audit Logs' }];
  readonly loading = signal(false);
  readonly detailLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly items = signal<AuditLogListItem[]>([]);
  readonly detail = signal<AuditLogDetail | null>(null);
  readonly selectedId = signal<string | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly totalCount = signal(0);
  readonly search = signal('');
  readonly action = signal('');
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
    this.action$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((value) => {
        this.action.set(value);
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

  onAction(value: string): void {
    this.action$.next(value);
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

  select(id: string): void {
    this.selectedId.set(id);
    this.detailLoading.set(true);
    this.audit.get(id).subscribe({
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

  shortId(id: string): string {
    return id.length > 8 ? `${id.slice(0, 8)}…` : id;
  }

  pretty(json?: string | null): string {
    if (!json) return '—';
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  }

  private toUtcIso(local: string): string | undefined {
    if (!local) return undefined;
    const date = new Date(local);
    return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.audit
      .list({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search().trim() || undefined,
        action: this.action().trim() || undefined,
        fromUtc: this.toUtcIso(this.fromLocal()),
        toUtc: this.toUtcIso(this.toLocal()),
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
