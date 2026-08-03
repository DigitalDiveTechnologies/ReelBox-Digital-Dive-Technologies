import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  OrphanScan,
  StorageSummary,
} from '../../core/api/models/admin-phase6.models';
import { SessionService } from '../../core/auth/session/session.service';
import { StorageService } from '../../core/services/storage.service';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

type PendingCleanup = 'selected' | 'all' | null;

@Component({
  selector: 'app-storage-page',
  standalone: true,
  imports: [
    DecimalPipe,
    FormsModule,
    PageHeaderComponent,
    PlaceholderCardComponent,
    EmptyStateComponent,
    ConfirmDialogComponent,
  ],
  template: `
    <app-page-header
      title="Storage"
      subtitle="Object storage summary, orphan scan, and cleanup"
      [breadcrumbs]="breadcrumbs"
    >
      @if (canManage()) {
        <button type="button" class="cta" [disabled]="scanning()" (click)="scan()">
          {{ scanning() ? 'Scanning…' : 'Orphan scan' }}
        </button>
      }
    </app-page-header>

    @if (error()) {
      <div class="banner error" role="alert">{{ error() }}</div>
    }
    @if (success()) {
      <div class="banner ok" role="status">{{ success() }}</div>
    }

    @if (loading()) {
      <div class="banner muted">Loading storage summary…</div>
    } @else if (summary()) {
      <section class="kpis">
        <article class="kpi">
          <span class="label">Provider</span>
          <strong>{{ summary()!.providerName }}</strong>
        </article>
        <article class="kpi">
          <span class="label">Media objects</span>
          <strong>{{ summary()!.mediaCount }}</strong>
        </article>
        <article class="kpi">
          <span class="label">Total bytes</span>
          <strong>{{ summary()!.totalBytes / 1048576 | number: '1.0-2' }} MB</strong>
        </article>
        <article class="kpi">
          <span class="label">Orphan estimate</span>
          <strong>{{ summary()!.orphanEstimate ?? '—' }}</strong>
        </article>
      </section>
    }

    <app-placeholder-card title="Orphan keys" hint="Select keys then cleanup">
      @if (!scanResult()) {
        <app-empty-state
          icon="▣"
          title="No scan yet"
          message="Run an orphan scan to list keys that are not referenced by media records."
        />
      } @else if (!scanResult()!.supported) {
        <div class="banner muted">
          {{ scanResult()!.message || 'Orphan scan is not supported for this storage provider.' }}
        </div>
      } @else if (scanResult()!.orphanKeys.length === 0) {
        <app-empty-state
          icon="✓"
          title="No orphans found"
          message="All storage keys appear to be referenced."
        />
      } @else {
        <div class="toolbar">
          <label class="check">
            <input
              type="checkbox"
              [checked]="allSelected()"
              (change)="toggleAll($event)"
              aria-label="Select all orphan keys"
            />
            Select all ({{ scanResult()!.orphanKeys.length }})
          </label>
          @if (canManage()) {
            <button
              type="button"
              [disabled]="selected().size === 0"
              (click)="ask('selected')"
            >
              Cleanup selected
            </button>
            <button type="button" class="warn" (click)="ask('all')">Cleanup all</button>
          }
        </div>
        <ul class="keys">
          @for (key of scanResult()!.orphanKeys; track key) {
            <li>
              <label>
                <input
                  type="checkbox"
                  [checked]="selected().has(key)"
                  (change)="toggleKey(key, $event)"
                  [attr.aria-label]="'Select ' + key"
                />
                <code>{{ key }}</code>
              </label>
            </li>
          }
        </ul>
      }
    </app-placeholder-card>

    <app-confirm-dialog
      [open]="pending() !== null"
      [title]="confirmTitle"
      [message]="confirmMessage"
      confirmLabel="Cleanup"
      [busy]="actionBusy()"
      (confirm)="runCleanup()"
      (cancel)="pending.set(null)"
    />
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
      .kpis {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
        gap: 0.85rem;
        margin-bottom: 1.15rem;
      }
      .kpi {
        padding: 1rem 1.05rem;
        border-radius: var(--radius);
        border: 1px solid color-mix(in srgb, var(--border) 70%, transparent);
        background: color-mix(in srgb, var(--surface) 92%, transparent);
      }
      .kpi .label {
        display: block;
        color: var(--text-muted);
        font-size: 0.75rem;
        margin-bottom: 0.35rem;
      }
      .kpi strong {
        font-size: 1.15rem;
        letter-spacing: -0.02em;
        word-break: break-word;
      }
      .toolbar {
        display: flex;
        flex-wrap: wrap;
        gap: 0.65rem;
        align-items: center;
        margin-bottom: 0.85rem;
      }
      .toolbar button {
        border: 1px solid var(--border);
        background: transparent;
        color: var(--text-primary);
        border-radius: 10px;
        padding: 0.45rem 0.75rem;
        cursor: pointer;
        font: inherit;
      }
      .toolbar button:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }
      .toolbar .warn {
        border-color: color-mix(in srgb, var(--status-fail) 50%, transparent);
        color: #fecaca;
      }
      .check {
        display: flex;
        align-items: center;
        gap: 0.45rem;
        color: var(--text-muted);
        font-size: 0.88rem;
      }
      .keys {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 0.45rem;
        max-height: 420px;
        overflow: auto;
      }
      .keys label {
        display: flex;
        gap: 0.55rem;
        align-items: flex-start;
        font-size: 0.82rem;
      }
      code {
        word-break: break-all;
        color: var(--text-primary);
      }
    `,
  ],
})
export class StoragePageComponent implements OnInit {
  private readonly storage = inject(StorageService);
  private readonly session = inject(SessionService);

  readonly breadcrumbs = [{ label: 'Storage' }];
  readonly loading = signal(false);
  readonly scanning = signal(false);
  readonly actionBusy = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly summary = signal<StorageSummary | null>(null);
  readonly scanResult = signal<OrphanScan | null>(null);
  readonly selected = signal(new Set<string>());
  readonly pending = signal<PendingCleanup>(null);

  canManage(): boolean {
    return this.session.hasAnyRole(['SuperAdmin', 'Operations', 'Technical']);
  }

  ngOnInit(): void {
    this.loadSummary();
  }

  allSelected(): boolean {
    const keys = this.scanResult()?.orphanKeys ?? [];
    return keys.length > 0 && keys.every((k) => this.selected().has(k));
  }

  toggleAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const next = new Set<string>();
    if (checked) {
      for (const key of this.scanResult()?.orphanKeys ?? []) next.add(key);
    }
    this.selected.set(next);
  }

  toggleKey(key: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const next = new Set(this.selected());
    if (checked) next.add(key);
    else next.delete(key);
    this.selected.set(next);
  }

  scan(): void {
    this.scanning.set(true);
    this.error.set(null);
    this.success.set(null);
    this.storage.orphanScan().subscribe({
      next: (result) => {
        this.scanResult.set(result);
        this.selected.set(new Set());
        this.scanning.set(false);
        this.success.set(
          result.supported
            ? `Scan complete — ${result.orphanKeys.length} orphan key(s).`
            : result.message || 'Scan unsupported.',
        );
      },
      error: (err: Error) => {
        this.scanning.set(false);
        this.error.set(err.message);
      },
    });
  }

  ask(mode: PendingCleanup): void {
    this.pending.set(mode);
  }

  get confirmTitle(): string {
    return this.pending() === 'all' ? 'Cleanup all orphans' : 'Cleanup selected orphans';
  }

  get confirmMessage(): string {
    const count =
      this.pending() === 'all'
        ? (this.scanResult()?.orphanKeys.length ?? 0)
        : this.selected().size;
    return `Delete ${count} orphan object(s) from storage? This cannot be undone.`;
  }

  runCleanup(): void {
    const mode = this.pending();
    if (!mode) return;
    const keys =
      mode === 'all'
        ? [...(this.scanResult()?.orphanKeys ?? [])]
        : [...this.selected()];
    if (keys.length === 0) {
      this.pending.set(null);
      return;
    }
    this.actionBusy.set(true);
    this.storage.cleanup({ keys }).subscribe({
      next: (result) => {
        this.actionBusy.set(false);
        this.pending.set(null);
        this.success.set(
          result.supported
            ? `Deleted ${result.deletedCount} object(s).`
            : result.message || 'Cleanup unsupported.',
        );
        this.scan();
        this.loadSummary();
      },
      error: (err: Error) => {
        this.actionBusy.set(false);
        this.pending.set(null);
        this.error.set(err.message);
      },
    });
  }

  private loadSummary(): void {
    this.loading.set(true);
    this.error.set(null);
    this.storage.summary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }
}
