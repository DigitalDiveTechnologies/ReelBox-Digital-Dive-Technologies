import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  template: `
    <div class="empty" role="status">
      <div class="icon" aria-hidden="true">{{ icon }}</div>
      <h3>{{ title }}</h3>
      <p>{{ message }}</p>
    </div>
  `,
  styles: [
    `
      .empty {
        text-align: center;
        padding: 2.5rem 1.25rem;
        color: var(--text-muted);
      }
      .icon {
        width: 52px;
        height: 52px;
        margin: 0 auto 1rem;
        border-radius: 14px;
        display: grid;
        place-items: center;
        background: var(--surface-elevated);
        border: 1px solid var(--border);
        font-size: 1.35rem;
      }
      h3 {
        margin: 0 0 0.4rem;
        color: var(--text-primary);
        font-size: 1.05rem;
      }
      p {
        margin: 0;
        max-width: 360px;
        margin-inline: auto;
        font-size: 0.9rem;
        line-height: 1.45;
      }
    `,
  ],
})
export class EmptyStateComponent {
  @Input() icon = '◇';
  @Input() title = 'Nothing here yet';
  @Input() message = 'This module will connect to Admin APIs in a later phase.';
}
