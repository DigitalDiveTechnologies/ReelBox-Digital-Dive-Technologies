import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { SessionService } from '../../core/auth/session/session.service';
import { NavService } from '../../core/services/nav.service';
import { ThemeToggleComponent } from '../../shared/components/theme-toggle/theme-toggle.component';

@Component({
  selector: 'app-top-navbar',
  standalone: true,
  imports: [ThemeToggleComponent],
  template: `
    <header class="topnav">
      <div class="left">
        <button type="button" class="icon-btn" (click)="nav.toggleSidebar()" aria-label="Toggle sidebar">
          ☰
        </button>
        <div class="context">
          <span class="eyebrow">Administration</span>
          <strong>Social Media Saver</strong>
        </div>
      </div>
      <div class="right">
        <app-theme-toggle />
        @if (session.admin(); as admin) {
          <span class="chip">{{ admin.email }}</span>
        } @else {
          <span class="chip">Authenticated</span>
        }
        <button type="button" class="ghost" (click)="signOut()" [disabled]="signingOut">
          {{ signingOut ? 'Signing out…' : 'Sign out' }}
        </button>
      </div>
    </header>
  `,
  styles: [
    `
      .topnav {
        height: var(--navbar-height);
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        padding: 0 1.1rem;
        border-bottom: 1px solid var(--border);
        background: var(--surface);
        backdrop-filter: blur(var(--glass-blur));
      }
      .left,
      .right {
        display: flex;
        align-items: center;
        gap: 0.85rem;
      }
      .icon-btn,
      .ghost {
        border: 1px solid color-mix(in srgb, var(--border) 70%, transparent);
        background: var(--surface-elevated);
        color: var(--text-primary);
        border-radius: 10px;
        cursor: pointer;
      }
      .icon-btn {
        width: 40px;
        height: 40px;
      }
      .ghost {
        padding: 0.45rem 0.85rem;
        font-size: 0.85rem;
      }
      .ghost:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
      .ghost:hover,
      .icon-btn:hover {
        border-color: color-mix(in srgb, var(--brand) 50%, var(--border));
      }
      .context {
        display: flex;
        flex-direction: column;
        line-height: 1.15;
      }
      .eyebrow {
        font-size: 0.7rem;
        color: var(--text-muted);
        text-transform: uppercase;
        letter-spacing: 0.06em;
      }
      .chip {
        font-size: 0.72rem;
        color: var(--ig-pink);
        border: 1px solid color-mix(in srgb, var(--ig-pink) 40%, transparent);
        background: color-mix(in srgb, var(--ig-pink) 12%, transparent);
        padding: 0.3rem 0.55rem;
        border-radius: 999px;
        max-width: 220px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
      @media (max-width: 640px) {
        .context strong {
          display: none;
        }
        .chip {
          display: none;
        }
      }
    `,
  ],
})
export class TopNavbarComponent {
  readonly nav = inject(NavService);
  readonly session = inject(SessionService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  signingOut = false;

  signOut(): void {
    if (this.signingOut) {
      return;
    }
    this.signingOut = true;
    this.auth.logout().subscribe({
      next: () => {
        this.signingOut = false;
        void this.router.navigateByUrl('/auth/login');
      },
      error: () => {
        this.signingOut = false;
        void this.router.navigateByUrl('/auth/login');
      },
    });
  }
}
