import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import {
  DownloadsTrendPoint,
  PlatformStatItem,
  ProviderPerformanceItem,
  UserActivityPoint,
} from '../../core/api/models/admin-phase6.models';
import {
  ReportExportType,
  ReportsService,
} from '../../core/services/reports.service';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { PlaceholderCardComponent } from '../../shared/components/placeholder-card/placeholder-card.component';

@Component({
  selector: 'app-reports-page',
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
      title="Reports"
      subtitle="Downloads, users, platforms, and provider performance"
      [breadcrumbs]="breadcrumbs"
    >
      <div class="exports">
        <button type="button" (click)="exportCsv('downloads')" [disabled]="exporting()">
          Export downloads CSV
        </button>
        <button type="button" (click)="exportCsv('users')" [disabled]="exporting()">
          Export users CSV
        </button>
        <button type="button" (click)="exportCsv('platforms')" [disabled]="exporting()">
          Export platforms CSV
        </button>
      </div>
    </app-page-header>

    @if (error()) {
      <div class="banner error" role="alert">{{ error() }}</div>
    }
    @if (success()) {
      <div class="banner ok" role="status">{{ success() }}</div>
    }
    @if (loading()) {
      <div class="banner muted">Loading reports…</div>
    } @else if (!error()) {
      <div class="grid">
        <app-placeholder-card title="Downloads trends" hint="Last 14 days">
          @if (downloads().length === 0) {
            <app-empty-state icon="▣" title="No download data" message="Trends appear after activity." />
          } @else {
            <div class="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Downloads</th>
                    <th>Failures</th>
                  </tr>
                </thead>
                <tbody>
                  @for (row of downloads(); track row.date) {
                    <tr>
                      <td>{{ row.date | date: 'mediumDate' }}</td>
                      <td>{{ row.downloads }}</td>
                      <td>{{ row.failures }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </app-placeholder-card>

        <app-placeholder-card title="User activity" hint="New users vs downloads">
          @if (users().length === 0) {
            <app-empty-state icon="👤" title="No user activity" message="Activity appears after sign-ups." />
          } @else {
            <div class="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>New users</th>
                    <th>Downloads</th>
                  </tr>
                </thead>
                <tbody>
                  @for (row of users(); track row.date) {
                    <tr>
                      <td>{{ row.date | date: 'mediumDate' }}</td>
                      <td>{{ row.newUsers }}</td>
                      <td>{{ row.downloads }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </app-placeholder-card>

        <app-placeholder-card title="Platform stats">
          @if (platforms().length === 0) {
            <app-empty-state icon="◎" title="No platform stats" message="Stats appear after media jobs." />
          } @else {
            <div class="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Platform</th>
                    <th>Total</th>
                    <th>Completed</th>
                    <th>Failed</th>
                    <th>Success</th>
                  </tr>
                </thead>
                <tbody>
                  @for (row of platforms(); track row.platform) {
                    <tr>
                      <td>{{ row.platform }}</td>
                      <td>{{ row.total }}</td>
                      <td>{{ row.completed }}</td>
                      <td>{{ row.failed }}</td>
                      <td>{{ row.successRate | number: '1.0-1' }}%</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </app-placeholder-card>

        <app-placeholder-card title="Provider performance">
          @if (providers().length === 0) {
            <app-empty-state icon="⬡" title="No provider data" message="Performance appears after resolves." />
          } @else {
            <div class="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Platform</th>
                    <th>Success</th>
                    <th>Fail</th>
                    <th>Rate</th>
                  </tr>
                </thead>
                <tbody>
                  @for (row of providers(); track row.platform) {
                    <tr>
                      <td>{{ row.platform }}</td>
                      <td>{{ row.success }}</td>
                      <td>{{ row.fail }}</td>
                      <td>{{ row.successRate | number: '1.0-1' }}%</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </app-placeholder-card>
      </div>
    }
  `,
  styles: [
    `
      .exports {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
      }
      .exports button {
        border: 1px solid var(--border);
        background: transparent;
        color: var(--text-primary);
        border-radius: 10px;
        padding: 0.45rem 0.75rem;
        cursor: pointer;
        font: inherit;
        font-size: 0.82rem;
      }
      .exports button:disabled {
        opacity: 0.55;
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
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
        gap: 1rem;
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
        padding: 0.55rem 0.35rem;
        border-bottom: 1px solid color-mix(in srgb, var(--border) 55%, transparent);
      }
      th {
        color: var(--text-muted);
        font-size: 0.72rem;
        text-transform: uppercase;
        letter-spacing: 0.04em;
      }
    `,
  ],
})
export class ReportsPageComponent implements OnInit {
  private readonly reports = inject(ReportsService);

  readonly breadcrumbs = [{ label: 'Reports' }];
  readonly loading = signal(true);
  readonly exporting = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly downloads = signal<DownloadsTrendPoint[]>([]);
  readonly users = signal<UserActivityPoint[]>([]);
  readonly platforms = signal<PlatformStatItem[]>([]);
  readonly providers = signal<ProviderPerformanceItem[]>([]);

  ngOnInit(): void {
    this.load();
  }

  exportCsv(type: ReportExportType): void {
    this.exporting.set(true);
    this.error.set(null);
    this.success.set(null);
    this.reports.downloadCsv(type).subscribe({
      next: (blob) => {
        this.saveBlob(blob, `report-${type}.csv`);
        this.exporting.set(false);
        this.success.set(`Downloaded report-${type}.csv`);
      },
      error: (err: Error) => {
        this.exporting.set(false);
        this.error.set(err.message);
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);
    forkJoin({
      downloads: this.reports.downloadsTrends(14),
      users: this.reports.userActivity(14),
      platforms: this.reports.platformStats(),
      providers: this.reports.providerPerformance(),
    }).subscribe({
      next: ({ downloads, users, platforms, providers }) => {
        this.downloads.set(downloads.items ?? []);
        this.users.set(users.items ?? []);
        this.platforms.set(platforms.items ?? []);
        this.providers.set(providers.items ?? []);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err.message);
        this.loading.set(false);
      },
    });
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
}
