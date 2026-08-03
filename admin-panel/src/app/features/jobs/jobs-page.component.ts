import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import {
  AdminMediaListItem,
  JobStatusCounts,
} from '../../core/api/models/admin-phase6.models';
import { SessionService } from '../../core/auth/session/session.service';
import { JobsService } from '../../core/services/jobs.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PaginationBarComponent } from '../../shared/components/pagination-bar/pagination-bar.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

type StatusGroup = 'all' | 'queued' | 'active' | 'completed' | 'failed';
type PendingAction = 'retry' | 'cancel' | 'requeue' | null;

const MANAGE_ROLES = ['SuperAdmin', 'Support', 'Operations'];

@Component({
  selector: 'app-jobs-page',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    PageHeaderComponent,
    PlaceholderCardComponent,
    EmptyStateComponent,
    PaginationBarComponent,
    ConfirmDialogComponent,
  ],
  template: `
    <app-page-header
      title="Download Jobs"
      subtitle="Queue operations — retry, cancel, or requeue jobs"
      [breadcrumbs]="breadcrumbs"
    />

    <app-placeholder-card title="Download Jobs list" hint="Grouped by pipeline status">
      <div class="tabs" role="tablist" aria-label="Job status groups">
        @for (tab of tabs; track tab.id) {
          <button
            type="button"
            role="tab"
            [attr.aria-selected]="statusGroup() === tab.id"
            [class.active]="statusGroup() === tab.id"
            (click)="setGroup(tab.id)"
          >
            {{ tab.label }}
            <span class="count">{{ countFor(tab.id) }}</span>
          </button>
        }
      </div>

      <div class="toolbar">
        <input
          type="search"
          placeholder="Search URL, title, email…"
          aria-label="Search jobs"
          [ngModel]="search()"
          (ngModelChange)="onSearch($event)"
        />
      </div>

      @if (error()) {
        <div class="banner error" role="alert">{{ error() }}</div>
      }
      @if (loading()) {
        <div class="banner muted">Loading jobs…</div>
      } @else if (!error() && items().length === 0) {
        <app-empty-state
          icon="⟳"
          title="No jobs found"
          message="Adjust the status tab or search query."
        />
      } @else if (!error()) {
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>
                  <button type="button" class="sort" (click)="toggleSort('createdAt')">
                    Created {{ sortMark('createdAt') }}
                  </button>
                </th>
                <th>Platform</th>
                <th>
                  <button type="button" class="sort" (click)="toggleSort('status')">
                    Status {{ sortMark('status') }}
                  </button>
                </th>
                <th>User</th>
                <th>Retries</th>
                <th>Error</th>
                @if (canManage()) {
                  <th>Actions</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (job of items(); track job.id) {
                <tr>
                  <td>{{ job.createdAt | date: 'short' }}</td>
                  <td>{{ job.platform }}</td>
                  <td>
                    <span class="badge" [attr.data-status]="job.status">{{ job.status }}</span>
                  </td>
                  <td>{{ job.userEmail || '—' }}</td>
                  <td>{{ job.retryCount }}</td>
                  <td>{{ job.errorCode || '—' }}</td>
                  @if (canManage()) {
                    <td class="row-actions">
                      <button type="button" (click)="ask(job, 'retry')">Retry</button>
                      <button type="button" (click)="ask(job, 'requeue')">Requeue</button>
                      <button type="button" class="warn" (click)="ask(job, 'cancel')">
                        Cancel
                      </button>
                    </td>
                  }
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
      }
    </app-placeholder-card>

    <app-confirm-dialog
      [open]="pending() !== null"
      [title]="confirmTitle"
      [message]="confirmMessage"
      [confirmLabel]="confirmLabel"
      [busy]="actionBusy()"
      (confirm)="runPending()"
      (cancel)="clearPending()"
    />
  `,
  styles: [
    `
      .tabs {
        display: flex;
        flex-wrap: wrap;
        gap: 0.45rem;
        margin-bottom: 1rem;
      }
      .tabs button {
        border: 1px solid var(--border);
        background: transparent;
        color: var(--text-muted);
        border-radius: 999px;
        padding: 0.4rem 0.75rem;
        cursor: pointer;
        font: inherit;
        font-size: 0.82rem;
      }
      .tabs button.active {
        color: var(--text-primary);
        border-color: color-mix(in srgb, var(--brand) 55%, var(--border));
        background: color-mix(in srgb, var(--brand) 12%, transparent);
      }
      .count {
        margin-left: 0.35rem;
        opacity: 0.75;
      }
      .toolbar {
        display: flex;
        flex-wrap: wrap;
        gap: 0.65rem;
        margin-bottom: 1rem;
      }
      input {
        border: 1px solid var(--border);
        background: var(--surface);
        color: var(--text-primary);
        border-radius: 10px;
        padding: 0.5rem 0.7rem;
        min-width: 220px;
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
      .row-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
      }
      .row-actions button {
        border: 1px solid var(--border);
        background: transparent;
        color: var(--text-primary);
        border-radius: 8px;
        padding: 0.3rem 0.55rem;
        cursor: pointer;
        font: inherit;
        font-size: 0.75rem;
      }
      .row-actions .warn {
        border-color: color-mix(in srgb, var(--status-fail) 50%, transparent);
        color: #fecaca;
      }
    `,
  ],
})
export class JobsPageComponent implements OnInit, OnDestroy {
  private readonly jobs = inject(JobsService);
  private readonly session = inject(SessionService);
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();

  readonly breadcrumbs = [{ label: 'Download Jobs' }];
  readonly tabs: { id: StatusGroup; label: string }[] = [
    { id: 'all', label: 'All' },
    { id: 'queued', label: 'Queued' },
    { id: 'active', label: 'Active' },
    { id: 'completed', label: 'Completed' },
    { id: 'failed', label: 'Failed' },
  ];
  readonly loading = signal(false);
  readonly actionBusy = signal(false);
  readonly error = signal<string | null>(null);
  readonly items = signal<AdminMediaListItem[]>([]);
  readonly counts = signal<JobStatusCounts>({
    queued: 0,
    active: 0,
    completed: 0,
    failed: 0,
    total: 0,
  });
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly totalCount = signal(0);
  readonly search = signal('');
  readonly statusGroup = signal<StatusGroup>('all');
  readonly sortBy = signal('createdAt');
  readonly sortDir = signal<'asc' | 'desc'>('desc');
  readonly pending = signal<PendingAction>(null);
  readonly pendingId = signal<string | null>(null);

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

  countFor(group: StatusGroup): number {
    const c = this.counts();
    switch (group) {
      case 'queued':
        return c.queued;
      case 'active':
        return c.active;
      case 'completed':
        return c.completed;
      case 'failed':
        return c.failed;
      default:
        return c.total;
    }
  }

  setGroup(group: StatusGroup): void {
    this.statusGroup.set(group);
    this.page.set(1);
    this.load();
  }

  onSearch(value: string): void {
    this.search$.next(value);
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

  ask(job: AdminMediaListItem, action: PendingAction): void {
    this.pendingId.set(job.id);
    this.pending.set(action);
  }

  clearPending(): void {
    this.pending.set(null);
    this.pendingId.set(null);
  }

  get confirmTitle(): string {
    switch (this.pending()) {
      case 'retry':
        return 'Retry job';
      case 'cancel':
        return 'Cancel job';
      case 'requeue':
        return 'Requeue job';
      default:
        return 'Confirm';
    }
  }

  get confirmMessage(): string {
    switch (this.pending()) {
      case 'retry':
        return 'Re-queue this failed job for another download attempt?';
      case 'cancel':
        return 'Mark this in-flight job as cancelled (Failed)?';
      case 'requeue':
        return 'Force requeue this job to Queued and publish a new download message?';
      default:
        return '';
    }
  }

  get confirmLabel(): string {
    switch (this.pending()) {
      case 'retry':
        return 'Retry';
      case 'cancel':
        return 'Cancel job';
      case 'requeue':
        return 'Requeue';
      default:
        return 'Confirm';
    }
  }

  runPending(): void {
    const id = this.pendingId();
    const action = this.pending();
    if (!id || !action) return;
    this.actionBusy.set(true);
    const done = () => {
      this.actionBusy.set(false);
      this.clearPending();
      this.load();
    };
    const fail = (err: Error) => {
      this.actionBusy.set(false);
      this.clearPending();
      this.error.set(err.message);
    };

    if (action === 'retry') {
      this.jobs.retry(id).subscribe({ next: done, error: fail });
    } else if (action === 'cancel') {
      this.jobs.cancel(id).subscribe({ next: done, error: fail });
    } else {
      this.jobs.requeue(id).subscribe({ next: done, error: fail });
    }
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.jobs
      .list({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search().trim() || undefined,
        statusGroup: this.statusGroup(),
        sortBy: this.sortBy(),
        sortDir: this.sortDir(),
      })
      .subscribe({
        next: (result) => {
          this.items.set(result.items ?? []);
          this.totalCount.set(result.totalCount ?? 0);
          this.counts.set(
            result.counts ?? {
              queued: 0,
              active: 0,
              completed: 0,
              failed: 0,
              total: 0,
            },
          );
          this.loading.set(false);
        },
        error: (err: Error) => {
          this.error.set(err.message);
          this.loading.set(false);
        },
      });
  }
}
