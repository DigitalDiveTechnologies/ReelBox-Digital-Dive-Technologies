import { Component, OnInit, inject, signal } from '@angular/core';
import {
  HealthComponentStatus,
  SystemHealthOverview,
} from '../../core/api/models/admin-phase6.models';
import { HealthService } from '../../core/services/health.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

@Component({
  selector: 'app-health-page',
  standalone: true,
  imports: [
    PageHeaderComponent,
    PlaceholderCardComponent,
    EmptyStateComponent,
  ],
  template: `
    <app-page-header
      title="System Health"
      subtitle="Operational probes across storage, queue, providers, and database"
      [breadcrumbs]="breadcrumbs"
    >
      <button type="button" class="cta" [disabled]="loading()" (click)="reload()">
        {{ loading() ? 'Refreshing…' : 'Refresh' }}
      </button>
    </app-page-header>

    @if (error()) {
      <div class="banner error" role="alert">{{ error() }}</div>
    }

    @if (loading() && !overview()) {
      <div class="banner muted">Loading health overview…</div>
    } @else if (overview()) {
      <section class="overall" [attr.data-status]="overview()!.overallStatus">
        <span class="label">Overall status</span>
        <strong>{{ overview()!.overallStatus }}</strong>
      </section>

      <app-placeholder-card title="Components" hint="Live probe results">
        @if (components().length === 0) {
          <app-empty-state
            icon="♥"
            title="No components"
            message="Health probe returned an empty component list."
          />
        } @else {
          <div class="grid">
            @for (c of components(); track c.name) {
              <article class="card" [attr.data-status]="c.status">
                <header>
                  <h3>{{ c.name }}</h3>
                  <span class="badge">{{ c.status }}</span>
                </header>
                @if (c.detail) {
                  <p>{{ c.detail }}</p>
                } @else {
                  <p class="muted">No additional detail.</p>
                }
              </article>
            }
          </div>
        }
      </app-placeholder-card>
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
      .banner.muted {
        border: 1px solid var(--border);
        color: var(--text-muted);
      }
      .overall {
        margin-bottom: 1.15rem;
        padding: 1.1rem 1.2rem;
        border-radius: var(--radius);
        border: 1px solid color-mix(in srgb, var(--border) 70%, transparent);
        background: color-mix(in srgb, var(--surface) 92%, transparent);
      }
      .overall .label {
        display: block;
        color: var(--text-muted);
        font-size: 0.75rem;
        margin-bottom: 0.35rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      .overall strong {
        font-size: 1.45rem;
        letter-spacing: -0.02em;
      }
      .overall[data-status='Healthy'],
      .overall[data-status='Ok'] {
        border-color: color-mix(in srgb, var(--status-ok) 45%, transparent);
      }
      .overall[data-status='Degraded'],
      .overall[data-status='Unhealthy'] {
        border-color: color-mix(in srgb, var(--status-fail) 45%, transparent);
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 0.85rem;
      }
      .card {
        border: 1px solid color-mix(in srgb, var(--border) 60%, transparent);
        border-radius: var(--radius);
        padding: 1rem;
        background: var(--surface);
      }
      .card header {
        display: flex;
        justify-content: space-between;
        gap: 0.5rem;
        align-items: center;
        margin-bottom: 0.55rem;
      }
      h3 {
        margin: 0;
        font-size: 0.95rem;
      }
      .badge {
        font-size: 0.72rem;
        padding: 0.15rem 0.5rem;
        border-radius: 999px;
        background: color-mix(in srgb, var(--status-queued) 25%, transparent);
      }
      .card[data-status='Healthy'] .badge,
      .card[data-status='Ok'] .badge {
        background: color-mix(in srgb, var(--status-ok) 25%, transparent);
      }
      .card[data-status='Degraded'] .badge,
      .card[data-status='Unhealthy'] .badge {
        background: color-mix(in srgb, var(--status-fail) 25%, transparent);
      }
      p {
        margin: 0;
        font-size: 0.84rem;
        color: var(--text-primary);
        line-height: 1.4;
        word-break: break-word;
      }
      .muted {
        color: var(--text-muted);
      }
    `,
  ],
})
export class HealthPageComponent implements OnInit {
  private readonly health = inject(HealthService);

  readonly breadcrumbs = [{ label: 'System Health' }];
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly overview = signal<SystemHealthOverview | null>(null);
  readonly components = signal<HealthComponentStatus[]>([]);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.health.overview().subscribe({
      next: (result) => {
        this.overview.set(result);
        this.components.set(result.components ?? []);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }
}
