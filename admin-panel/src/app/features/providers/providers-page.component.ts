import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProviderAdminItem } from '../../core/api/models/admin-phase6.models';
import { SessionService } from '../../core/auth/session/session.service';
import { ProvidersAdminService } from '../../core/services/providers-admin.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

interface ProviderDraft extends ProviderAdminItem {
  dirty: boolean;
  saving: boolean;
  probing: boolean;
}

@Component({
  selector: 'app-providers-page',
  standalone: true,
  imports: [
    FormsModule,
    PageHeaderComponent,
    PlaceholderCardComponent,
    EmptyStateComponent,
  ],
  template: `
    <app-page-header
      title="Providers"
      subtitle="Timeouts, priority, enablement, and health probes"
      [breadcrumbs]="breadcrumbs"
    />

    @if (error()) {
      <div class="banner error" role="alert">{{ error() }}</div>
    }
    @if (success()) {
      <div class="banner ok" role="status">{{ success() }}</div>
    }

    <app-placeholder-card title="Providers list" hint="Secrets are configured server-side only">
      <p class="note">Secrets are configured server-side only</p>

      @if (loading()) {
        <div class="banner muted">Loading providers…</div>
      } @else if (!error() && drafts().length === 0) {
        <app-empty-state
          icon="⬡"
          title="No providers"
          message="Provider configuration was empty."
        />
      } @else if (!error()) {
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Platform</th>
                <th>Enabled</th>
                <th>Timeout</th>
                <th>Priority</th>
                <th>Resolver</th>
                <th>Health</th>
                <th>Token</th>
                <th>RapidAPI</th>
                @if (canManage()) {
                  <th>Actions</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (row of drafts(); track row.name) {
                <tr>
                  <td>{{ row.name }}</td>
                  <td>{{ row.platform }}</td>
                  <td>
                    <input
                      type="checkbox"
                      [ngModel]="row.enabled"
                      (ngModelChange)="mark(row, 'enabled', $event)"
                      [disabled]="!canManage()"
                      [attr.aria-label]="'Enabled ' + row.name"
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      min="1"
                      class="num"
                      [ngModel]="row.timeoutSeconds"
                      (ngModelChange)="mark(row, 'timeoutSeconds', $event)"
                      [disabled]="!canManage()"
                      [attr.aria-label]="'Timeout for ' + row.name"
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      min="0"
                      class="num"
                      [ngModel]="row.priority"
                      (ngModelChange)="mark(row, 'priority', $event)"
                      [disabled]="!canManage()"
                      [attr.aria-label]="'Priority for ' + row.name"
                    />
                  </td>
                  <td>{{ row.resolver }}</td>
                  <td>
                    <span class="badge" [attr.data-health]="row.health">{{ row.health }}</span>
                  </td>
                  <td>{{ row.hasAccessToken ? 'Yes' : 'No' }}</td>
                  <td>{{ row.hasRapidApiKey ? 'Yes' : 'No' }}</td>
                  @if (canManage()) {
                    <td class="actions">
                      <button
                        type="button"
                        [disabled]="!row.dirty || row.saving"
                        (click)="save(row)"
                      >
                        {{ row.saving ? 'Saving…' : 'Save' }}
                      </button>
                      <button
                        type="button"
                        [disabled]="row.probing"
                        (click)="probe(row)"
                      >
                        {{ row.probing ? 'Checking…' : 'Health check' }}
                      </button>
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </app-placeholder-card>
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
      .banner.ok {
        border: 1px solid color-mix(in srgb, var(--status-ok) 45%, transparent);
        background: color-mix(in srgb, var(--status-ok) 14%, transparent);
      }
      .banner.muted {
        border: 1px solid var(--border);
        color: var(--text-muted);
      }
      .note {
        margin: 0 0 1rem;
        color: var(--text-muted);
        font-size: 0.85rem;
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
        padding: 0.55rem 0.4rem;
        border-bottom: 1px solid color-mix(in srgb, var(--border) 55%, transparent);
        vertical-align: middle;
      }
      th {
        color: var(--text-muted);
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      .num {
        width: 72px;
        border: 1px solid var(--border);
        background: var(--surface);
        color: var(--text-primary);
        border-radius: 8px;
        padding: 0.35rem 0.45rem;
        font: inherit;
      }
      .badge {
        display: inline-block;
        padding: 0.15rem 0.5rem;
        border-radius: 999px;
        font-size: 0.75rem;
        background: color-mix(in srgb, var(--status-queued) 25%, transparent);
      }
      .badge[data-health='Healthy'],
      .badge[data-health='Ok'] {
        background: color-mix(in srgb, var(--status-ok) 25%, transparent);
      }
      .badge[data-health='Degraded'],
      .badge[data-health='Unhealthy'] {
        background: color-mix(in srgb, var(--status-fail) 25%, transparent);
      }
      .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
      }
      .actions button {
        border: 1px solid var(--border);
        background: transparent;
        color: var(--text-primary);
        border-radius: 8px;
        padding: 0.35rem 0.6rem;
        cursor: pointer;
        font: inherit;
        font-size: 0.75rem;
      }
      .actions button:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
    `,
  ],
})
export class ProvidersPageComponent implements OnInit {
  private readonly providers = inject(ProvidersAdminService);
  private readonly session = inject(SessionService);

  readonly breadcrumbs = [{ label: 'Providers' }];
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly drafts = signal<ProviderDraft[]>([]);

  canManage(): boolean {
    return this.session.hasAnyRole(['SuperAdmin', 'Technical', 'Operations']);
  }

  ngOnInit(): void {
    this.load();
  }

  mark(
    row: ProviderDraft,
    field: 'enabled' | 'timeoutSeconds' | 'priority',
    value: boolean | number | string,
  ): void {
    if (field === 'enabled') {
      row.enabled = Boolean(value);
    } else if (field === 'timeoutSeconds') {
      row.timeoutSeconds = Number(value) || 1;
    } else {
      row.priority = Number(value) || 0;
    }
    row.dirty = true;
    this.drafts.set([...this.drafts()]);
  }

  save(row: ProviderDraft): void {
    row.saving = true;
    this.drafts.set([...this.drafts()]);
    this.error.set(null);
    this.success.set(null);
    this.providers
      .update(row.name, {
        enabled: row.enabled,
        timeoutSeconds: row.timeoutSeconds,
        priority: row.priority,
      })
      .subscribe({
        next: (item) => {
          Object.assign(row, item, { dirty: false, saving: false, probing: false });
          this.drafts.set([...this.drafts()]);
          this.success.set(`Saved ${item.name}.`);
        },
        error: (err: Error) => {
          row.saving = false;
          this.drafts.set([...this.drafts()]);
          this.error.set(err.message);
        },
      });
  }

  probe(row: ProviderDraft): void {
    row.probing = true;
    this.drafts.set([...this.drafts()]);
    this.error.set(null);
    this.providers.healthCheck(row.name).subscribe({
      next: (result) => {
        row.health = result.status;
        row.probing = false;
        this.drafts.set([...this.drafts()]);
        this.success.set(
          `${row.name}: ${result.status}${result.detail ? ' — ' + result.detail : ''}`,
        );
      },
      error: (err: Error) => {
        row.probing = false;
        this.drafts.set([...this.drafts()]);
        this.error.set(err.message);
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.providers.list().subscribe({
      next: (items) => {
        this.drafts.set(
          (items ?? []).map((i) => ({
            ...i,
            dirty: false,
            saving: false,
            probing: false,
          })),
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
