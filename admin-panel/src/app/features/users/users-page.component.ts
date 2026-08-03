import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import {
  MobileUserDetail,
  MobileUserListItem,
} from '../../core/api/models/admin-modules.models';
import { UsersService } from '../../core/services/users.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PaginationBarComponent } from '../../shared/components/pagination-bar/pagination-bar.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

type PendingAction = 'block' | 'unblock' | 'revoke' | null;

@Component({
  selector: 'app-users-page',
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
      title="Users"
      subtitle="Mobile app accounts — block, unblock, revoke sessions"
      [breadcrumbs]="breadcrumbs"
    />

    <app-placeholder-card title="Users list" hint="Server-side search & pagination">
      <div class="toolbar">
        <input
          type="search"
          placeholder="Search email…"
          aria-label="Search users"
          [ngModel]="search()"
          (ngModelChange)="onSearch($event)"
        />
        <select [ngModel]="statusFilter()" (ngModelChange)="onStatusFilter($event)">
          <option value="all">All statuses</option>
          <option value="active">Active</option>
          <option value="blocked">Blocked</option>
        </select>
      </div>

      @if (error()) {
        <div class="banner error" role="alert">{{ error() }}</div>
      }
      @if (loading()) {
        <div class="banner muted">Loading users…</div>
      } @else if (!error() && items().length === 0) {
        <app-empty-state
          icon="👤"
          title="No users found"
          message="Try clearing search or status filters."
        />
      } @else if (!error()) {
        <div class="layout">
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>
                    <button type="button" class="sort" (click)="toggleSort('email')">
                      Email {{ sortMark('email') }}
                    </button>
                  </th>
                  <th>
                    <button type="button" class="sort" (click)="toggleSort('status')">
                      Status {{ sortMark('status') }}
                    </button>
                  </th>
                  <th>
                    <button type="button" class="sort" (click)="toggleSort('createdAt')">
                      Registered {{ sortMark('createdAt') }}
                    </button>
                  </th>
                  <th>Media</th>
                </tr>
              </thead>
              <tbody>
                @for (user of items(); track user.id) {
                  <tr
                    [class.selected]="selectedId() === user.id"
                    (click)="selectUser(user.id)"
                  >
                    <td>{{ user.email }}</td>
                    <td>
                      <span class="badge" [class.off]="!user.isActive">
                        {{ user.isActive ? 'Active' : 'Blocked' }}
                      </span>
                    </td>
                    <td>{{ user.createdAt | date: 'mediumDate' }}</td>
                    <td>{{ user.mediaCount }}</td>
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
              <h3>User details</h3>
              <dl>
                <div><dt>Email</dt><dd>{{ detail()!.email }}</dd></div>
                <div><dt>Status</dt><dd>{{ detail()!.isActive ? 'Active' : 'Blocked' }}</dd></div>
                <div><dt>Registered</dt><dd>{{ detail()!.createdAt | date: 'medium' }}</dd></div>
                <div><dt>Updated</dt><dd>{{ detail()!.updatedAt | date: 'medium' }}</dd></div>
                <div><dt>Media</dt><dd>{{ detail()!.mediaCount }}</dd></div>
                <div>
                  <dt>Session</dt>
                  <dd>{{ detail()!.hasActiveSession ? 'Active refresh token' : 'None' }}</dd>
                </div>
              </dl>
              <div class="actions">
                @if (detail()!.isActive) {
                  <button type="button" class="warn" (click)="ask('block')">Block</button>
                } @else {
                  <button type="button" class="ok" (click)="ask('unblock')">Unblock</button>
                }
                <button type="button" (click)="ask('revoke')">Revoke sessions</button>
              </div>
            } @else {
              <app-empty-state
                icon="◇"
                title="Select a user"
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
        min-width: 180px;
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
        background: color-mix(in srgb, var(--status-ok) 25%, transparent);
        font-size: 0.75rem;
      }
      .badge.off {
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
      .actions .ok {
        border-color: color-mix(in srgb, var(--status-ok) 50%, transparent);
      }
      .muted {
        color: var(--text-muted);
      }
    `,
  ],
})
export class UsersPageComponent implements OnInit, OnDestroy {
  private readonly users = inject(UsersService);
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();

  readonly breadcrumbs = [{ label: 'Users' }];
  readonly loading = signal(false);
  readonly detailLoading = signal(false);
  readonly actionBusy = signal(false);
  readonly error = signal<string | null>(null);
  readonly items = signal<MobileUserListItem[]>([]);
  readonly detail = signal<MobileUserDetail | null>(null);
  readonly selectedId = signal<string | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly totalCount = signal(0);
  readonly search = signal('');
  readonly statusFilter = signal<'all' | 'active' | 'blocked'>('all');
  readonly sortBy = signal('createdAt');
  readonly sortDir = signal<'asc' | 'desc'>('desc');
  readonly pending = signal<PendingAction>(null);

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

  onStatusFilter(value: 'all' | 'active' | 'blocked'): void {
    this.statusFilter.set(value);
    this.page.set(1);
    this.load();
  }

  toggleSort(column: string): void {
    if (this.sortBy() === column) {
      this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(column);
      this.sortDir.set(column === 'email' ? 'asc' : 'desc');
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

  selectUser(id: string): void {
    this.selectedId.set(id);
    this.detailLoading.set(true);
    this.users.get(id).subscribe({
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

  ask(action: PendingAction): void {
    this.pending.set(action);
  }

  get confirmTitle(): string {
    switch (this.pending()) {
      case 'block':
        return 'Block user';
      case 'unblock':
        return 'Unblock user';
      case 'revoke':
        return 'Revoke sessions';
      default:
        return 'Confirm';
    }
  }

  get confirmMessage(): string {
    switch (this.pending()) {
      case 'block':
        return 'Blocked users cannot sign in. Active sessions will be revoked.';
      case 'unblock':
        return 'This user will be allowed to sign in again.';
      case 'revoke':
        return 'All refresh tokens for this user will be cleared.';
      default:
        return '';
    }
  }

  get confirmLabel(): string {
    switch (this.pending()) {
      case 'block':
        return 'Block';
      case 'unblock':
        return 'Unblock';
      case 'revoke':
        return 'Revoke';
      default:
        return 'Confirm';
    }
  }

  runPending(): void {
    const id = this.selectedId();
    const action = this.pending();
    if (!id || !action) return;
    this.actionBusy.set(true);
    const done = () => {
      this.actionBusy.set(false);
      this.pending.set(null);
      this.load();
      this.selectUser(id);
    };
    const fail = (err: Error) => {
      this.actionBusy.set(false);
      this.pending.set(null);
      this.error.set(err.message);
    };

    if (action === 'revoke') {
      this.users.revokeSessions(id).subscribe({ next: done, error: fail });
      return;
    }
    this.users
      .updateStatus(id, action === 'unblock')
      .subscribe({ next: done, error: fail });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    const status = this.statusFilter();
    this.users
      .list({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search().trim() || undefined,
        isActive: status === 'all' ? null : status === 'active',
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
