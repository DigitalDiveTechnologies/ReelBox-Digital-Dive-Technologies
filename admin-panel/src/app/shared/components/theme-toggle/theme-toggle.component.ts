import { Component, inject } from '@angular/core';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  template: `
    <button
      type="button"
      class="theme-btn"
      (click)="themeService.toggle()"
      [attr.aria-label]="themeService.isDark() ? 'Switch to light mode' : 'Switch to dark mode'"
      [title]="themeService.isDark() ? 'Light mode' : 'Dark mode'"
    >
      @if (themeService.isDark()) {
        <svg viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <circle cx="12" cy="12" r="4" />
          <path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" />
        </svg>
      } @else {
        <svg viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <path d="M21 14.5A8.5 8.5 0 1 1 9.5 3a7 7 0 0 0 11.5 11.5z" />
        </svg>
      }
    </button>
  `,
  styles: [
    `
      :host {
        display: inline-flex;
        flex-shrink: 0;
      }
      .theme-btn {
        width: 40px;
        height: 40px;
        display: grid;
        place-items: center;
        padding: 0;
        border: 1px solid var(--border, rgba(14, 11, 20, 0.1));
        border-radius: 10px;
        background: #ffffff;
        color: var(--text-primary, #0e0b14);
        cursor: pointer;
        transition: border-color 0.15s ease, background 0.15s ease;
      }
      :host-context(html[data-theme='dark']) .theme-btn {
        background: var(--surface-elevated, rgba(22, 19, 32, 0.85));
        color: var(--text-primary, #ffffff);
      }
      .theme-btn:hover {
        border-color: color-mix(in srgb, var(--brand, #dd2a7b) 50%, var(--border, #ccc));
      }
      .theme-btn:focus-visible {
        outline: 2px solid var(--brand, #dd2a7b);
        outline-offset: 2px;
      }
      .theme-btn svg {
        width: 18px;
        height: 18px;
        display: block;
      }
    `,
  ],
})
export class ThemeToggleComponent {
  readonly themeService = inject(ThemeService);
}
