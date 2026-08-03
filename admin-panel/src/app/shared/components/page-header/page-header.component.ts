import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

export interface BreadcrumbItem {
  label: string;
  route?: string;
}

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [RouterLink],
  template: `
    <header class="page-header">
      <nav class="crumbs" aria-label="Breadcrumb">
        <a routerLink="/dashboard">Admin</a>
        @for (c of breadcrumbs; track c.label; let last = $last) {
          <span class="sep">/</span>
          @if (c.route && !last) {
            <a [routerLink]="c.route">{{ c.label }}</a>
          } @else {
            <span [class.current]="last">{{ c.label }}</span>
          }
        }
      </nav>
      <div class="title-row">
        <div>
          <h1>{{ title }}</h1>
          @if (subtitle) {
            <p class="sub">{{ subtitle }}</p>
          }
        </div>
        <ng-content />
      </div>
    </header>
  `,
  styles: [
    `
      .page-header {
        margin-bottom: 1.5rem;
      }
      .crumbs {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        gap: 0.35rem;
        font-size: 0.8rem;
        color: var(--text-muted);
        margin-bottom: 0.65rem;
      }
      .crumbs a:hover {
        color: var(--brand);
      }
      .sep {
        opacity: 0.5;
      }
      .current {
        color: var(--text-primary);
      }
      .title-row {
        display: flex;
        flex-wrap: wrap;
        align-items: flex-start;
        justify-content: space-between;
        gap: 1rem;
      }
      h1 {
        margin: 0;
        font-size: 1.65rem;
        font-weight: 700;
        letter-spacing: -0.03em;
      }
      .sub {
        margin: 0.35rem 0 0;
        color: var(--text-muted);
        font-size: 0.95rem;
      }
    `,
  ],
})
export class PageHeaderComponent {
  @Input({ required: true }) title!: string;
  @Input() subtitle = '';
  @Input() breadcrumbs: BreadcrumbItem[] = [];
}
