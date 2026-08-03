import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-placeholder-card',
  standalone: true,
  template: `
    <section class="card">
      <div class="card-head">
        <h2>{{ title }}</h2>
        @if (hint) {
          <span class="hint">{{ hint }}</span>
        }
      </div>
      <div class="card-body">
        <ng-content />
      </div>
    </section>
  `,
  styles: [
    `
      .card {
        background: var(--surface);
        border: 1px solid var(--border);
        border-radius: var(--radius-lg);
        box-shadow: var(--shadow);
        backdrop-filter: blur(var(--glass-blur));
        overflow: hidden;
      }
      .card-head {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        padding: 1rem 1.15rem;
        border-bottom: 1px solid var(--border);
      }
      h2 {
        margin: 0;
        font-size: 0.95rem;
        font-weight: 600;
      }
      .hint {
        font-size: 0.75rem;
        color: var(--text-muted);
      }
      .card-body {
        padding: 1rem 1.15rem 1.25rem;
      }
    `,
  ],
})
export class PlaceholderCardComponent {
  @Input({ required: true }) title!: string;
  @Input() hint = '';
}
