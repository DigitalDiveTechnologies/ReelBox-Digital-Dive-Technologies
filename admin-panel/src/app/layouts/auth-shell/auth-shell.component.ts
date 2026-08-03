import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeToggleComponent } from '../../shared/components/theme-toggle/theme-toggle.component';

@Component({
  selector: 'app-auth-shell',
  standalone: true,
  imports: [RouterOutlet, ThemeToggleComponent],
  template: `
    <div class="auth-shell">
      <div class="theme-slot">
        <app-theme-toggle />
      </div>
      <div class="panel">
        <div class="brand">
          <div class="mark" aria-hidden="true">▶</div>
          <div>
            <strong>ReelBox Admin</strong>
            <p>Secure operations portal</p>
          </div>
        </div>
        <router-outlet />
      </div>
    </div>
  `,
  styles: [
    `
      .auth-shell {
        min-height: 100vh;
        display: grid;
        place-items: center;
        padding: 1.5rem;
        position: relative;
      }
      .theme-slot {
        position: absolute;
        top: 1rem;
        right: 1rem;
      }
      .panel {
        width: min(420px, 100%);
        background: var(--surface);
        border: 1px solid var(--border);
        border-radius: var(--radius-lg);
        box-shadow: var(--shadow);
        backdrop-filter: blur(var(--glass-blur));
        padding: 1.5rem;
      }
      .brand {
        display: flex;
        align-items: center;
        gap: 0.85rem;
        margin-bottom: 1.5rem;
      }
      .mark {
        width: 42px;
        height: 42px;
        border-radius: 12px;
        display: grid;
        place-items: center;
        background: var(--mark-gradient);
        color: #ffffff;
      }
      .brand strong {
        display: block;
        font-size: 1.1rem;
      }
      .brand p {
        margin: 0.15rem 0 0;
        color: var(--text-muted);
        font-size: 0.85rem;
      }
    `,
  ],
})
export class AuthShellComponent {}
