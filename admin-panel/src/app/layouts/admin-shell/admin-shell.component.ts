import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavService } from '../../core/services/nav.service';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { TopNavbarComponent } from '../top-navbar/top-navbar.component';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopNavbarComponent],
  template: `
    <div class="shell">
      @if (nav.mobileOpen()) {
        <button
          type="button"
          class="nav-backdrop"
          aria-label="Close navigation"
          (click)="nav.closeMobileNav()"
        ></button>
      }
      <app-sidebar />
      <div class="main">
        <app-top-navbar />
        <main class="content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [
    `
      .shell {
        display: flex;
        min-height: 100vh;
        width: 100%;
      }
      .nav-backdrop {
        position: fixed;
        inset: 0;
        z-index: 40;
        border: none;
        padding: 0;
        margin: 0;
        background: rgba(0, 0, 0, 0.45);
        cursor: pointer;
      }
      .main {
        flex: 1;
        min-width: 0;
        display: flex;
        flex-direction: column;
      }
      .content {
        flex: 1;
        padding: 1.25rem 1.35rem 2rem;
        max-width: 1400px;
        width: 100%;
        margin: 0 auto;
      }
      @media (max-width: 640px) {
        .content {
          padding: 1rem;
        }
      }
    `,
  ],
})
export class AdminShellComponent {
  readonly nav = inject(NavService);
}
