import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { AdminAccountListItem } from '../../core/api/models/admin-modules.models';
import { SessionService } from '../../core/auth/session/session.service';
import { AdminsService } from '../../core/services/admins.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PaginationBarComponent } from '../../shared/components/pagination-bar/pagination-bar.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

const ROLES = ['SuperAdmin', 'Operations', 'Support', 'Technical', 'Analyst'];

@Component({
  selector: 'app-admins-page',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    ReactiveFormsModule,
    PageHeaderComponent,
    PlaceholderCardComponent,
    EmptyStateComponent,
    PaginationBarComponent,
    ConfirmDialogComponent,
  ],
  template: `
    <app-page-header
      title="Admin Users"
      subtitle="Create, update, activate, and deactivate administrators"
      [breadcrumbs]="breadcrumbs"
    >
      @if (canManage()) {
        <button type="button" class="cta" (click)="openCreate()">Create admin</button>
      }
    </app-page-header>

    <app-placeholder-card title="Admin Users list" hint="SuperAdmin manage actions">
      <div class="toolbar">
        <input
          type="search"
          placeholder="Search email or name…"
          aria-label="Search administrators"
          [ngModel]="search()"
          (ngModelChange)="onSearch($event)"
        />
        <select [ngModel]="roleFilter()" (ngModelChange)="onRoleFilter($event)">
          <option value="">All roles</option>
          @for (role of roles; track role) {
            <option [value]="role">{{ role }}</option>
          }
        </select>
        <select [ngModel]="statusFilter()" (ngModelChange)="onStatusFilter($event)">
          <option value="all">All statuses</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>
      </div>

      @if (error()) {
        <div class="banner error" role="alert">{{ error() }}</div>
      }
      @if (loading()) {
        <div class="banner muted">Loading administrators…</div>
      } @else if (!error() && items().length === 0) {
        <app-empty-state
          icon="★"
          title="No administrators"
          message="Create the first admin account or adjust filters."
        />
      } @else if (!error()) {
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
                  <button type="button" class="sort" (click)="toggleSort('displayName')">
                    Name {{ sortMark('displayName') }}
                  </button>
                </th>
                <th>
                  <button type="button" class="sort" (click)="toggleSort('role')">
                    Role {{ sortMark('role') }}
                  </button>
                </th>
                <th>
                  <button type="button" class="sort" (click)="toggleSort('status')">
                    Status {{ sortMark('status') }}
                  </button>
                </th>
                <th>
                  <button type="button" class="sort" (click)="toggleSort('createdAt')">
                    Created {{ sortMark('createdAt') }}
                  </button>
                </th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (admin of items(); track admin.id) {
                <tr>
                  <td>{{ admin.email }}</td>
                  <td>{{ admin.displayName || '—' }}</td>
                  <td>{{ admin.role }}</td>
                  <td>
                    <span class="badge" [class.off]="!admin.isActive">
                      {{ admin.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td>{{ admin.createdAt | date: 'mediumDate' }}</td>
                  <td class="row-actions">
                    @if (canManage()) {
                      <button type="button" (click)="openEdit(admin)">Edit</button>
                      @if (admin.isActive) {
                        <button type="button" class="warn" (click)="askToggle(admin, false)">
                          Deactivate
                        </button>
                      } @else {
                        <button type="button" (click)="askToggle(admin, true)">Activate</button>
                      }
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
      }
    </app-placeholder-card>

    @if (editorOpen()) {
      <div class="backdrop" (click)="closeEditor()"></div>
      <div class="dialog" role="dialog" aria-modal="true">
        <h2>{{ editingId() ? 'Update admin' : 'Create admin' }}</h2>
        <form [formGroup]="form" (ngSubmit)="save()">
          <label>
            Email
            <input type="email" formControlName="email" [readonly]="!!editingId()" />
          </label>
          @if (!editingId()) {
            <label>
              Password
              <input type="password" formControlName="password" autocomplete="new-password" />
            </label>
          }
          <label>
            Display name
            <input type="text" formControlName="displayName" />
          </label>
          <label>
            Role
            <select formControlName="role">
              @for (role of roles; track role) {
                <option [value]="role">{{ role }}</option>
              }
            </select>
          </label>
          @if (formError()) {
            <div class="banner error">{{ formError() }}</div>
          }
          <div class="actions">
            <button type="button" class="ghost" (click)="closeEditor()" [disabled]="saving()">
              Cancel
            </button>
            <button type="submit" class="cta" [disabled]="saving() || form.invalid">
              {{ saving() ? 'Saving…' : 'Save' }}
            </button>
          </div>
        </form>
      </div>
    }

    <app-confirm-dialog
      [open]="toggleTarget() !== null"
      [title]="toggleTarget()?.isActive === false ? 'Activate admin' : 'Deactivate admin'"
      [message]="
        toggleTarget()?.isActive === false
          ? 'This administrator will be able to sign in again.'
          : 'Deactivated admins cannot sign in; their refresh tokens are cleared.'
      "
      [confirmLabel]="toggleTarget()?.isActive === false ? 'Activate' : 'Deactivate'"
      [busy]="saving()"
      (confirm)="runToggle()"
      (cancel)="toggleTarget.set(null)"
    />
  `,
  styles: [
    `
      .cta {
        border: none;
        background: linear-gradient(135deg, var(--brand), var(--brand-deep));
        color: #fff;
        border-radius: 10px;
        padding: 0.55rem 0.95rem;
        cursor: pointer;
        font: inherit;
      }
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
      .toolbar input,
      .toolbar select,
      form input,
      form select {
        border: 1px solid var(--border);
        background: var(--surface);
        color: var(--text-primary);
        border-radius: 10px;
        padding: 0.5rem 0.7rem;
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
        background: color-mix(in srgb, var(--status-ok) 25%, transparent);
        font-size: 0.75rem;
      }
      .badge.off {
        background: color-mix(in srgb, var(--status-fail) 25%, transparent);
      }
      .row-actions {
        display: flex;
        gap: 0.35rem;
        flex-wrap: wrap;
      }
      .row-actions button {
        border: 1px solid var(--border);
        background: transparent;
        color: var(--text-primary);
        border-radius: 8px;
        padding: 0.3rem 0.55rem;
        cursor: pointer;
      }
      .row-actions .warn {
        border-color: color-mix(in srgb, var(--status-fail) 50%, transparent);
        color: #fecaca;
      }
      .backdrop {
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.55);
        z-index: 40;
      }
      .dialog {
        position: fixed;
        z-index: 50;
        left: 50%;
        top: 50%;
        transform: translate(-50%, -50%);
        width: min(440px, calc(100vw - 2rem));
        padding: 1.25rem 1.35rem;
        border-radius: var(--radius-lg);
        background: var(--surface-elevated);
        border: 1px solid var(--border);
        box-shadow: var(--shadow);
      }
      .dialog h2 {
        margin: 0 0 1rem;
        font-size: 1.05rem;
      }
      form label {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
        margin-bottom: 0.75rem;
        font-size: 0.8rem;
        color: var(--text-muted);
      }
      .actions {
        display: flex;
        justify-content: flex-end;
        gap: 0.55rem;
        margin-top: 0.5rem;
      }
      .ghost {
        border: 1px solid var(--border);
        background: transparent;
        color: var(--text-primary);
        border-radius: 10px;
        padding: 0.5rem 0.85rem;
        cursor: pointer;
      }
    `,
  ],
})
export class AdminsPageComponent implements OnInit, OnDestroy {
  private readonly admins = inject(AdminsService);
  private readonly session = inject(SessionService);
  private readonly fb = inject(FormBuilder);
  private readonly destroy$ = new Subject<void>();
  private readonly search$ = new Subject<string>();

  readonly roles = ROLES;
  readonly breadcrumbs = [{ label: 'Admin Users' }];
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly formError = signal<string | null>(null);
  readonly items = signal<AdminAccountListItem[]>([]);
  readonly page = signal(1);
  readonly pageSize = signal(25);
  readonly totalCount = signal(0);
  readonly search = signal('');
  readonly roleFilter = signal('');
  readonly statusFilter = signal<'all' | 'active' | 'inactive'>('all');
  readonly sortBy = signal('createdAt');
  readonly sortDir = signal<'asc' | 'desc'>('desc');
  readonly editorOpen = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly toggleTarget = signal<AdminAccountListItem | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    displayName: [''],
    role: ['Support', Validators.required],
  });

  canManage(): boolean {
    return this.session.hasAnyRole(['SuperAdmin']);
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

  onRoleFilter(value: string): void {
    this.roleFilter.set(value);
    this.page.set(1);
    this.load();
  }

  onStatusFilter(value: 'all' | 'active' | 'inactive'): void {
    this.statusFilter.set(value);
    this.page.set(1);
    this.load();
  }

  toggleSort(column: string): void {
    if (this.sortBy() === column) {
      this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortBy.set(column);
      this.sortDir.set(column === 'email' || column === 'displayName' ? 'asc' : 'desc');
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

  openCreate(): void {
    this.editingId.set(null);
    this.form.reset({
      email: '',
      password: '',
      displayName: '',
      role: 'Support',
    });
    this.form.controls.password.setValidators([
      Validators.required,
      Validators.minLength(8),
    ]);
    this.form.controls.password.updateValueAndValidity();
    this.form.controls.email.enable();
    this.formError.set(null);
    this.editorOpen.set(true);
  }

  openEdit(admin: AdminAccountListItem): void {
    this.editingId.set(admin.id);
    this.form.reset({
      email: admin.email,
      password: '',
      displayName: admin.displayName ?? '',
      role: admin.role,
    });
    this.form.controls.password.clearValidators();
    this.form.controls.password.updateValueAndValidity();
    this.form.controls.email.disable();
    this.formError.set(null);
    this.editorOpen.set(true);
  }

  closeEditor(): void {
    this.editorOpen.set(false);
    this.form.controls.email.enable();
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.formError.set(null);
    const raw = this.form.getRawValue();
    const id = this.editingId();
    const request$ = id
      ? this.admins.update(id, {
          displayName: raw.displayName || null,
          role: raw.role,
        })
      : this.admins.create({
          email: raw.email,
          password: raw.password,
          displayName: raw.displayName || null,
          role: raw.role,
        });

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.closeEditor();
        this.load();
      },
      error: (err: Error) => {
        this.saving.set(false);
        this.formError.set(err.message);
      },
    });
  }

  askToggle(admin: AdminAccountListItem, _activate: boolean): void {
    this.toggleTarget.set(admin);
  }

  runToggle(): void {
    const target = this.toggleTarget();
    if (!target) return;
    this.saving.set(true);
    this.admins.update(target.id, { isActive: !target.isActive }).subscribe({
      next: () => {
        this.saving.set(false);
        this.toggleTarget.set(null);
        this.load();
      },
      error: (err: Error) => {
        this.saving.set(false);
        this.toggleTarget.set(null);
        this.error.set(err.message);
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    const status = this.statusFilter();
    this.admins
      .list({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search().trim() || undefined,
        role: this.roleFilter() || null,
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
