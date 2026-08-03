import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminAccountListItem, RoleDefinition } from '../../core/api/models/admin-modules.models';
import { SessionService } from '../../core/auth/session/session.service';
import { AdminsService } from '../../core/services/admins.service';
import { RolesService } from '../../core/services/roles.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

@Component({
  selector: 'app-roles-page',
  standalone: true,
  imports: [
    FormsModule,
    PageHeaderComponent,
    PlaceholderCardComponent,
    EmptyStateComponent,
    ConfirmDialogComponent,
  ],
  template: `
    <app-page-header
      title="Roles"
      subtitle="Role catalog and assignments — permission editor comes later"
      [breadcrumbs]="breadcrumbs"
    />

    @if (error()) {
      <div class="banner error" role="alert">{{ error() }}</div>
    }

    <div class="grid">
      <app-placeholder-card title="Role definitions" hint="Permissions foundation">
        @if (loadingRoles()) {
          <div class="banner muted">Loading roles…</div>
        } @else if (roles().length === 0) {
          <app-empty-state
            icon="⚿"
            title="No roles"
            message="Role catalog was empty."
          />
        } @else {
          <div class="roles">
            @for (role of roles(); track role.name) {
              <article class="role">
                <header>
                  <h3>{{ role.name }}</h3>
                  <span class="count">{{ role.permissions.length }} permissions</span>
                </header>
                <p>{{ role.description }}</p>
                <ul>
                  @for (permission of role.permissions; track permission) {
                    <li>{{ permission }}</li>
                  }
                </ul>
              </article>
            }
          </div>
        }
      </app-placeholder-card>

      <app-placeholder-card title="Assign role" hint="SuperAdmin only">
        @if (!canAssign()) {
          <app-empty-state
            icon="★"
            title="Read-only"
            message="Only SuperAdmin can assign roles."
          />
        } @else {
          @if (loadingAdmins()) {
            <div class="banner muted">Loading admins…</div>
          } @else {
            <label>
              Administrator
              <select [ngModel]="selectedAdminId()" (ngModelChange)="selectedAdminId.set($event)">
                <option value="">Select admin…</option>
                @for (admin of admins(); track admin.id) {
                  <option [value]="admin.id">
                    {{ admin.email }} ({{ admin.role }})
                  </option>
                }
              </select>
            </label>
            <label>
              New role
              <select [ngModel]="selectedRole()" (ngModelChange)="selectedRole.set($event)">
                @for (role of roles(); track role.name) {
                  <option [value]="role.name">{{ role.name }}</option>
                }
              </select>
            </label>
            <button
              type="button"
              class="cta"
              [disabled]="!selectedAdminId() || !selectedRole() || assigning()"
              (click)="askAssign()"
            >
              {{ assigning() ? 'Assigning…' : 'Assign role' }}
            </button>
          }
        }
      </app-placeholder-card>
    </div>

    <app-confirm-dialog
      [open]="confirmOpen()"
      title="Assign role"
      [message]="'Change this administrator role to ' + selectedRole() + '?'"
      confirmLabel="Assign"
      [busy]="assigning()"
      (confirm)="assign()"
      (cancel)="confirmOpen.set(false)"
    />
  `,
  styles: [
    `
      .banner {
        margin-bottom: 1rem;
        padding: 0.75rem 0.9rem;
        border-radius: 12px;
        font-size: 0.88rem;
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
      .grid {
        display: grid;
        grid-template-columns: minmax(0, 1.5fr) minmax(280px, 1fr);
        gap: 1rem;
      }
      @media (max-width: 960px) {
        .grid {
          grid-template-columns: 1fr;
        }
      }
      .roles {
        display: grid;
        gap: 0.85rem;
      }
      .role {
        border: 1px solid color-mix(in srgb, var(--border) 60%, transparent);
        border-radius: var(--radius);
        padding: 0.9rem 1rem;
        background: var(--surface);
      }
      .role header {
        display: flex;
        justify-content: space-between;
        gap: 0.75rem;
        align-items: baseline;
      }
      .role h3 {
        margin: 0;
        font-size: 0.95rem;
      }
      .count {
        font-size: 0.75rem;
        color: var(--text-muted);
      }
      .role p {
        margin: 0.45rem 0 0.65rem;
        color: var(--text-muted);
        font-size: 0.85rem;
      }
      .role ul {
        margin: 0;
        padding-left: 1.1rem;
        color: var(--text-muted);
        font-size: 0.8rem;
        column-count: 2;
      }
      @media (max-width: 700px) {
        .role ul {
          column-count: 1;
        }
      }
      label {
        display: flex;
        flex-direction: column;
        gap: 0.35rem;
        margin-bottom: 0.85rem;
        font-size: 0.8rem;
        color: var(--text-muted);
      }
      select,
      button {
        font: inherit;
      }
      select {
        border: 1px solid var(--border);
        background: var(--surface);
        color: var(--text-primary);
        border-radius: 10px;
        padding: 0.5rem 0.7rem;
      }
      .cta {
        border: none;
        background: linear-gradient(135deg, var(--brand), var(--brand-deep));
        color: #fff;
        border-radius: 10px;
        padding: 0.55rem 0.95rem;
        cursor: pointer;
      }
      .cta:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
    `,
  ],
})
export class RolesPageComponent implements OnInit {
  private readonly rolesApi = inject(RolesService);
  private readonly adminsApi = inject(AdminsService);
  private readonly session = inject(SessionService);

  readonly breadcrumbs = [{ label: 'Roles' }];
  readonly loadingRoles = signal(true);
  readonly loadingAdmins = signal(false);
  readonly assigning = signal(false);
  readonly error = signal<string | null>(null);
  readonly roles = signal<RoleDefinition[]>([]);
  readonly admins = signal<AdminAccountListItem[]>([]);
  readonly selectedAdminId = signal('');
  readonly selectedRole = signal('Support');
  readonly confirmOpen = signal(false);

  canAssign(): boolean {
    return this.session.hasAnyRole(['SuperAdmin']);
  }

  ngOnInit(): void {
    this.rolesApi.list().subscribe({
      next: (response) => {
        this.roles.set(response.items ?? []);
        if (this.roles().length > 0) {
          this.selectedRole.set(this.roles()[0].name);
        }
        this.loadingRoles.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loadingRoles.set(false);
      },
    });

    if (this.canAssign()) {
      this.loadingAdmins.set(true);
      this.adminsApi.list({ page: 1, pageSize: 100 }).subscribe({
        next: (result) => {
          this.admins.set(result.items ?? []);
          this.loadingAdmins.set(false);
        },
        error: (err: Error) => {
          this.error.set(err.message);
          this.loadingAdmins.set(false);
        },
      });
    }
  }

  askAssign(): void {
    if (!this.selectedAdminId() || !this.selectedRole()) return;
    this.confirmOpen.set(true);
  }

  assign(): void {
    const adminId = this.selectedAdminId();
    const role = this.selectedRole();
    if (!adminId || !role) return;
    this.assigning.set(true);
    this.rolesApi.assign(adminId, role).subscribe({
      next: (updated) => {
        this.admins.update((list) =>
          list.map((a) => (a.id === updated.id ? updated : a)),
        );
        this.assigning.set(false);
        this.confirmOpen.set(false);
      },
      error: (err: Error) => {
        this.assigning.set(false);
        this.confirmOpen.set(false);
        this.error.set(err.message);
      },
    });
  }
}
