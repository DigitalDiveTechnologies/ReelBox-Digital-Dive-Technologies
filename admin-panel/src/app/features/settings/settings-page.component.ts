import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  SettingItem,
  SettingsGrouped,
} from '../../core/api/models/admin-phase6.models';
import { SessionService } from '../../core/auth/session/session.service';
import { SettingsService } from '../../core/services/settings.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [
    FormsModule,
    PageHeaderComponent,
    PlaceholderCardComponent,
    EmptyStateComponent,
  ],
  template: `
    <app-page-header
      title="Settings"
      subtitle="Allowlisted operational settings grouped by category"
      [breadcrumbs]="breadcrumbs"
    >
      @if (canEdit()) {
        <button
          type="button"
          class="cta"
          [disabled]="saving() || !dirty()"
          (click)="save()"
        >
          {{ saving() ? 'Saving…' : 'Save' }}
        </button>
      }
    </app-page-header>

    @if (error()) {
      <div class="banner error" role="alert">{{ error() }}</div>
    }
    @if (success()) {
      <div class="banner ok" role="status">{{ success() }}</div>
    }
    @if (!canEdit()) {
      <div class="banner muted" role="status">
        Read-only — only SuperAdmin can update settings.
      </div>
    }

    @if (loading()) {
      <div class="banner muted">Loading settings…</div>
    } @else if (!error() && groupEntries().length === 0) {
      <app-empty-state
        icon="⚙"
        title="No settings"
        message="Allowlisted settings will appear here once configured."
      />
    } @else if (!error()) {
      <div class="groups">
        @for (entry of groupEntries(); track entry[0]) {
          <app-placeholder-card [title]="entry[0]" [hint]="entry[1].length + ' keys'">
            <div class="form">
              @for (item of entry[1]; track item.key) {
                <label>
                  <span class="key">{{ item.key }}</span>
                  <input
                    type="text"
                    [ngModel]="values()[item.key]"
                    (ngModelChange)="onValue(item.key, $event)"
                    [disabled]="!canEdit()"
                    [attr.aria-label]="'Setting ' + item.key"
                  />
                </label>
              }
            </div>
          </app-placeholder-card>
        }
      </div>
    }
  `,
  styles: [
    `
      .cta {
        border: none;
        border-radius: 10px;
        padding: 0.55rem 0.95rem;
        cursor: pointer;
        color: #fff;
        background: linear-gradient(135deg, var(--brand), var(--brand-deep));
        font: inherit;
      }
      .cta:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
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
      .groups {
        display: grid;
        gap: 1rem;
      }
      .form {
        display: grid;
        gap: 0.75rem;
      }
      label {
        display: grid;
        gap: 0.35rem;
      }
      .key {
        font-size: 0.78rem;
        color: var(--text-muted);
        font-family: ui-monospace, monospace;
        word-break: break-all;
      }
      input {
        border: 1px solid var(--border);
        background: var(--surface);
        color: var(--text-primary);
        border-radius: 10px;
        padding: 0.55rem 0.7rem;
        font: inherit;
        width: 100%;
        max-width: 520px;
      }
      input:disabled {
        opacity: 0.75;
      }
    `,
  ],
})
export class SettingsPageComponent implements OnInit {
  private readonly settings = inject(SettingsService);
  private readonly session = inject(SessionService);

  readonly breadcrumbs = [{ label: 'Settings' }];
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly dirty = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly groups = signal<Record<string, SettingItem[]>>({});
  readonly values = signal<Record<string, string>>({});
  private original: Record<string, string> = {};

  canEdit(): boolean {
    return this.session.hasRole('SuperAdmin');
  }

  groupEntries(): [string, SettingItem[]][] {
    return Object.entries(this.groups());
  }

  ngOnInit(): void {
    this.load();
  }

  onValue(key: string, value: string): void {
    const next = { ...this.values(), [key]: value };
    this.values.set(next);
    this.dirty.set(
      Object.keys(this.original).some((k) => next[k] !== this.original[k]),
    );
  }

  save(): void {
    if (!this.canEdit()) return;
    this.saving.set(true);
    this.error.set(null);
    this.success.set(null);
    this.settings.put({ settings: this.values() }).subscribe({
      next: (result) => {
        this.apply(result);
        this.saving.set(false);
        this.dirty.set(false);
        this.success.set('Settings saved.');
      },
      error: (err: Error) => {
        this.saving.set(false);
        this.error.set(err.message);
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.settings.get().subscribe({
      next: (result) => {
        this.apply(result);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  private apply(result: SettingsGrouped): void {
    const groups = result.groups ?? {};
    this.groups.set(groups);
    const flat: Record<string, string> = {};
    for (const items of Object.values(groups)) {
      for (const item of items ?? []) {
        flat[item.key] = item.value ?? '';
      }
    }
    this.original = { ...flat };
    this.values.set(flat);
    this.dirty.set(false);
  }
}
