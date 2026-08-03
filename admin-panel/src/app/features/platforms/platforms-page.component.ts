import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PlatformAdminItem } from '../../core/api/models/admin-phase6.models';
import { SessionService } from '../../core/auth/session/session.service';
import { PlatformsService } from '../../core/services/platforms.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

interface PlatformDraft {
  platform: string;
  enabled: boolean;
  maintenanceMode: boolean;
  dailyLimit: number;
  status: string;
  dirty: boolean;
  saving: boolean;
}

@Component({
  selector: 'app-platforms-page',
  standalone: true,
  imports: [
    FormsModule,
    PageHeaderComponent,
    PlaceholderCardComponent,
    EmptyStateComponent,
  ],
  template: `
    <app-page-header
      title="Platforms"
      subtitle="Enable platforms, set daily limits, and toggle global maintenance"
      [breadcrumbs]="breadcrumbs"
    />

    @if (error()) {
      <div class="banner error" role="alert">{{ error() }}</div>
    }
    @if (success()) {
      <div class="banner ok" role="status">{{ success() }}</div>
    }

    <app-placeholder-card title="Platform controls" hint="PATCH /admin/platforms/{platform}">
      @if (loading()) {
        <div class="banner muted">Loading platforms…</div>
      } @else if (!error() && drafts().length === 0) {
        <app-empty-state
          icon="◎"
          title="No platforms"
          message="Platform configuration was empty."
        />
      } @else if (!error()) {
        <div class="grid">
          @for (draft of drafts(); track draft.platform) {
            <article class="card">
              <header>
                <h3>{{ draft.platform }}</h3>
                <span class="status" [attr.data-status]="draft.status">{{ draft.status }}</span>
              </header>

              <label class="toggle">
                <input
                  type="checkbox"
                  [ngModel]="draft.enabled"
                  (ngModelChange)="setEnabled(draft, $event)"
                  [disabled]="!canManage()"
                />
                <span>Enabled</span>
              </label>

              <label class="toggle">
                <input
                  type="checkbox"
                  [ngModel]="draft.maintenanceMode"
                  (ngModelChange)="setMaintenance(draft, $event)"
                  [disabled]="!canManage()"
                />
                <span>Global maintenance</span>
              </label>

              <label class="field">
                <span>Daily limit</span>
                <input
                  type="number"
                  min="0"
                  [attr.aria-label]="'Daily limit for ' + draft.platform"
                  [ngModel]="draft.dailyLimit"
                  (ngModelChange)="setDailyLimit(draft, $event)"
                  [disabled]="!canManage()"
                />
              </label>

              @if (canManage()) {
                <button
                  type="button"
                  class="save"
                  [disabled]="!draft.dirty || draft.saving"
                  (click)="save(draft)"
                >
                  {{ draft.saving ? 'Saving…' : 'Save' }}
                </button>
              }
            </article>
          }
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
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
        gap: 1rem;
      }
      .card {
        border: 1px solid color-mix(in srgb, var(--border) 60%, transparent);
        border-radius: var(--radius);
        padding: 1.1rem;
        background: var(--surface);
        display: grid;
        gap: 0.85rem;
      }
      header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 0.75rem;
      }
      h3 {
        margin: 0;
        text-transform: capitalize;
        font-size: 1.05rem;
      }
      .status {
        font-size: 0.72rem;
        padding: 0.2rem 0.55rem;
        border-radius: 999px;
        background: color-mix(in srgb, var(--status-queued) 25%, transparent);
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      .status[data-status='enabled'] {
        background: color-mix(in srgb, var(--status-ok) 25%, transparent);
      }
      .status[data-status='disabled'],
      .status[data-status='maintenance'] {
        background: color-mix(in srgb, var(--status-fail) 25%, transparent);
      }
      .toggle {
        display: flex;
        align-items: center;
        gap: 0.55rem;
        font-size: 0.9rem;
        color: var(--text-muted);
      }
      .field {
        display: grid;
        gap: 0.35rem;
        font-size: 0.78rem;
        color: var(--text-muted);
      }
      .field input {
        border: 1px solid var(--border);
        background: var(--surface-elevated);
        color: var(--text-primary);
        border-radius: 10px;
        padding: 0.5rem 0.7rem;
        font: inherit;
        max-width: 160px;
      }
      .save {
        justify-self: start;
        border: none;
        border-radius: 10px;
        padding: 0.5rem 0.9rem;
        cursor: pointer;
        color: #fff;
        background: linear-gradient(135deg, var(--brand), var(--brand-deep));
        font: inherit;
      }
      .save:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
    `,
  ],
})
export class PlatformsPageComponent implements OnInit {
  private readonly platforms = inject(PlatformsService);
  private readonly session = inject(SessionService);

  readonly breadcrumbs = [{ label: 'Platforms' }];
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly drafts = signal<PlatformDraft[]>([]);

  canManage(): boolean {
    return this.session.hasAnyRole(['SuperAdmin', 'Operations', 'Technical']);
  }

  ngOnInit(): void {
    this.load();
  }

  setEnabled(draft: PlatformDraft, value: boolean): void {
    draft.enabled = value;
    draft.dirty = true;
    this.drafts.set([...this.drafts()]);
  }

  setMaintenance(draft: PlatformDraft, value: boolean): void {
    draft.maintenanceMode = value;
    draft.dirty = true;
    this.drafts.set([...this.drafts()]);
  }

  setDailyLimit(draft: PlatformDraft, value: number | string): void {
    draft.dailyLimit = Number(value) || 0;
    draft.dirty = true;
    this.drafts.set([...this.drafts()]);
  }

  save(draft: PlatformDraft): void {
    draft.saving = true;
    this.drafts.set([...this.drafts()]);
    this.error.set(null);
    this.success.set(null);
    this.platforms
      .update(draft.platform, {
        enabled: draft.enabled,
        dailyLimit: draft.dailyLimit,
        maintenanceMode: draft.maintenanceMode,
      })
      .subscribe({
        next: (item) => {
          Object.assign(draft, this.toDraft(item));
          draft.dirty = false;
          draft.saving = false;
          this.drafts.set([...this.drafts()]);
          this.success.set(`Saved ${item.platform} settings.`);
        },
        error: (err: Error) => {
          draft.saving = false;
          this.drafts.set([...this.drafts()]);
          this.error.set(err.message);
        },
      });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.platforms.list().subscribe({
      next: (items) => {
        this.drafts.set((items ?? []).map((i) => this.toDraft(i)));
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  private toDraft(item: PlatformAdminItem): PlatformDraft {
    return {
      platform: item.platform,
      enabled: item.enabled,
      maintenanceMode: item.maintenanceMode,
      dailyLimit: item.dailyLimit,
      status: item.status,
      dirty: false,
      saving: false,
    };
  }
}
