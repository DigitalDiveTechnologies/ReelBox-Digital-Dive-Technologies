import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import {
  DashboardActivityItem,
  DashboardSummary,
  DashboardTrendPoint,
} from '../../core/api/models/admin-modules.models';
import { DashboardService } from '../../core/services/dashboard.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    PageHeaderComponent,
    PlaceholderCardComponent,
    EmptyStateComponent,
  ],
  template: `
    <app-page-header
      title="Dashboard"
      subtitle="Operational summary across users, media, and admins"
      [breadcrumbs]="breadcrumbs"
    />

    @if (error()) {
      <div class="banner error" role="alert">{{ error() }}</div>
    }

    @if (loading()) {
      <div class="banner muted">Loading dashboard…</div>
    } @else if (!error()) {
      <section class="kpis">
        @for (card of kpiCards(); track card.label) {
          <article class="kpi">
            <span class="label">{{ card.label }}</span>
            <strong>{{ card.value }}</strong>
          </article>
        }
      </section>

      <div class="grid">
        <app-placeholder-card title="Download trends" hint="Last 14 days">
          @if (trends().length === 0) {
            <app-empty-state
              icon="▣"
              title="No trend data"
              message="Charts appear once download activity is recorded."
            />
          } @else {
            <div class="chart" role="img" aria-label="Downloads and failures chart">
              @for (point of trends(); track point.date) {
                <div class="bar-group" [title]="point.date + ': ' + point.downloads + ' ok / ' + point.failures + ' fail'">
                  <div class="bars">
                    <span class="bar ok" [style.height.%]="barHeight(point.downloads)"></span>
                    <span class="bar fail" [style.height.%]="barHeight(point.failures)"></span>
                  </div>
                  <span class="day">{{ point.date | date: 'MMM d' }}</span>
                </div>
              }
            </div>
            <div class="legend">
              <span><i class="dot ok"></i> Downloads</span>
              <span><i class="dot fail"></i> Failures</span>
            </div>
          }
        </app-placeholder-card>

        <app-placeholder-card title="System summary">
          <ul class="summary">
            <li><span>Total media</span><strong>{{ summary()?.totalMedia ?? 0 }}</strong></li>
            <li><span>Completed</span><strong>{{ summary()?.completedMedia ?? 0 }}</strong></li>
            <li><span>Failed</span><strong>{{ summary()?.failedMedia ?? 0 }}</strong></li>
            <li><span>Success rate</span><strong>{{ summary()?.successRate ?? 0 | number: '1.0-1' }}%</strong></li>
            <li><span>Active admins</span><strong>{{ summary()?.activeAdmins ?? 0 }}</strong></li>
          </ul>
        </app-placeholder-card>
      </div>

      <app-placeholder-card title="Recent activity" hint="Latest admin + media events">
        @if (activity().length === 0) {
          <app-empty-state
            icon="◇"
            title="No recent activity"
            message="Privileged actions and media events will show here."
          />
        } @else {
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Type</th>
                  <th>Title</th>
                  <th>When</th>
                </tr>
              </thead>
              <tbody>
                @for (item of activity(); track item.id) {
                  <tr>
                    <td><span class="pill">{{ item.type }}</span></td>
                    <td>{{ item.title }}</td>
                    <td>{{ item.createdAt | date: 'medium' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </app-placeholder-card>
    }
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
        color: var(--status-fail);
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
        border: 1px solid var(--border);
        background: var(--surface);
        backdrop-filter: blur(var(--glass-blur));
      }
      .kpi .label {
        display: block;
        color: var(--text-muted);
        font-size: 0.75rem;
        margin-bottom: 0.35rem;
      }
      .kpi strong {
        font-size: 1.35rem;
        letter-spacing: -0.02em;
      }
      .grid {
        display: grid;
        grid-template-columns: minmax(0, 2fr) minmax(0, 1fr);
        gap: 1rem;
        margin-bottom: 1rem;
      }
      @media (max-width: 900px) {
        .grid {
          grid-template-columns: 1fr;
        }
      }
      .chart {
        display: flex;
        align-items: flex-end;
        gap: 0.35rem;
        min-height: 180px;
        overflow-x: auto;
        padding-bottom: 0.25rem;
      }
      .bar-group {
        flex: 1 0 28px;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.35rem;
      }
      .bars {
        height: 140px;
        display: flex;
        align-items: flex-end;
        gap: 3px;
      }
      .bar {
        width: 8px;
        min-height: 2px;
        border-radius: 4px 4px 0 0;
      }
      .bar.ok {
        background: var(--status-ok);
      }
      .bar.fail {
        background: var(--status-fail);
      }
      .day {
        font-size: 0.65rem;
        color: var(--text-muted);
        writing-mode: vertical-rl;
        transform: rotate(180deg);
        max-height: 48px;
      }
      .legend {
        display: flex;
        gap: 1rem;
        margin-top: 0.75rem;
        font-size: 0.78rem;
        color: var(--text-muted);
      }
      .dot {
        display: inline-block;
        width: 8px;
        height: 8px;
        border-radius: 50%;
        margin-right: 0.35rem;
      }
      .dot.ok {
        background: var(--status-ok);
      }
      .dot.fail {
        background: var(--status-fail);
      }
      .summary {
        list-style: none;
        margin: 0;
        padding: 0;
        display: grid;
        gap: 0.65rem;
      }
      .summary li {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        font-size: 0.88rem;
        color: var(--text-muted);
      }
      .summary strong {
        color: var(--text-primary);
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
        padding: 0.65rem 0.4rem;
        border-bottom: 1px solid color-mix(in srgb, var(--border) 55%, transparent);
      }
      th {
        color: var(--text-muted);
        font-weight: 600;
        font-size: 0.75rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
      .pill {
        display: inline-block;
        padding: 0.15rem 0.45rem;
        border-radius: 999px;
        border: 1px solid var(--border);
        font-size: 0.72rem;
        color: var(--text-muted);
      }
    `,
  ],
})
export class DashboardPageComponent implements OnInit {
  private readonly dashboard = inject(DashboardService);

  readonly breadcrumbs = [{ label: 'Dashboard' }];
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly summary = signal<DashboardSummary | null>(null);
  readonly trends = signal<DashboardTrendPoint[]>([]);
  readonly activity = signal<DashboardActivityItem[]>([]);
  private maxTrend = 1;

  kpiCards = signal<{ label: string; value: string }[]>([]);

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      summary: this.dashboard.getSummary(),
      trends: this.dashboard.getTrends(14),
      activity: this.dashboard.getActivity(20),
    }).subscribe({
      next: ({ summary, trends, activity }) => {
        this.summary.set(summary);
        this.trends.set(trends.items ?? []);
        this.activity.set(activity.items ?? []);
        this.maxTrend = Math.max(
          1,
          ...this.trends().flatMap((p) => [p.downloads, p.failures]),
        );
        this.kpiCards.set([
          { label: 'Total users', value: String(summary.totalUsers) },
          { label: 'Active users', value: String(summary.activeUsers) },
          { label: 'Blocked users', value: String(summary.blockedUsers) },
          { label: 'Downloads today', value: String(summary.downloadsToday) },
          {
            label: 'Success rate',
            value: `${Number(summary.successRate).toFixed(1)}%`,
          },
          { label: 'Active admins', value: String(summary.activeAdmins) },
        ]);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  barHeight(value: number): number {
    return Math.max(4, Math.round((value / this.maxTrend) * 100));
  }
}
