import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavService } from '../../core/services/nav.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <aside
      class="sidebar"
      [class.collapsed]="nav.sidebarCollapsed()"
      [class.mobile-open]="nav.mobileOpen()"
    >
      <div class="brand">
        <div class="mark" aria-hidden="true">▶</div>
        @if (!nav.sidebarCollapsed()) {
          <div class="brand-text">
            <strong>ReelBox</strong>
            <span>Admin</span>
          </div>
        }
      </div>

      <nav class="nav" aria-label="Admin modules">
        @for (group of sections; track group) {
          @if (!nav.sidebarCollapsed()) {
            <div class="section-label">{{ group }}</div>
          }
          @for (item of itemsFor(group); track item.route) {
            <a
              class="nav-item"
              [routerLink]="item.route"
              routerLinkActive="active"
              [title]="item.label"
              (click)="nav.closeMobileNav()"
            >
              <span class="ico" aria-hidden="true">{{ iconGlyph(item.icon) }}</span>
              @if (!nav.sidebarCollapsed()) {
                <span class="label">{{ item.label }}</span>
              }
            </a>
          }
        }
      </nav>
    </aside>
  `,
  styles: [
    `
      .sidebar {
        width: var(--sidebar-width);
        flex-shrink: 0;
        height: 100%;
        display: flex;
        flex-direction: column;
        background: var(--surface);
        border-right: 1px solid var(--border);
        backdrop-filter: blur(var(--glass-blur));
        transition: width 0.2s ease, transform 0.2s ease;
        overflow: hidden;
      }
      .sidebar.collapsed {
        width: var(--sidebar-collapsed);
      }
      .brand {
        height: var(--navbar-height);
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 0 1rem;
        border-bottom: 1px solid color-mix(in srgb, var(--border) 55%, transparent);
      }
      .mark {
        width: 36px;
        height: 36px;
        border-radius: 10px;
        display: grid;
        place-items: center;
        background: var(--mark-gradient);
        color: #ffffff;
        font-size: 0.85rem;
        flex-shrink: 0;
      }
      .brand-text {
        display: flex;
        flex-direction: column;
        line-height: 1.15;
      }
      .brand-text strong {
        font-size: 0.95rem;
      }
      .brand-text span {
        font-size: 0.72rem;
        color: var(--text-muted);
      }
      .nav {
        padding: 0.85rem 0.65rem 1.25rem;
        overflow-y: auto;
        flex: 1;
      }
      .section-label {
        margin: 0.85rem 0.55rem 0.35rem;
        font-size: 0.68rem;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--text-muted);
        font-weight: 600;
      }
      .nav-item {
        display: flex;
        align-items: center;
        gap: 0.7rem;
        padding: 0.65rem 0.7rem;
        border-radius: 12px;
        color: var(--text-muted);
        margin-bottom: 0.15rem;
        transition: background 0.15s ease, color 0.15s ease;
      }
      .nav-item:hover {
        background: color-mix(in srgb, var(--surface-elevated) 80%, transparent);
        color: var(--text-primary);
      }
      .nav-item.active {
        background: color-mix(in srgb, var(--ig-pink) 18%, transparent);
        color: var(--text-primary);
        box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--ig-pink) 40%, transparent);
      }
      .ico {
        width: 1.4rem;
        text-align: center;
        flex-shrink: 0;
        font-size: 0.95rem;
      }
      .label {
        font-size: 0.88rem;
        font-weight: 500;
        white-space: nowrap;
      }
      .collapsed .nav-item {
        justify-content: center;
        padding-inline: 0.5rem;
      }
      @media (max-width: 900px) {
        .sidebar {
          position: fixed;
          z-index: 45;
          left: 0;
          top: 0;
          height: 100vh;
          width: min(var(--sidebar-width), 86vw);
          transform: translateX(-105%);
          box-shadow: var(--shadow);
        }
        .sidebar.mobile-open {
          transform: translateX(0);
        }
        .sidebar.collapsed {
          width: min(var(--sidebar-width), 86vw);
        }
      }
    `,
  ],
})
export class SidebarComponent {
  readonly nav = inject(NavService);

  readonly sections = ['Overview', 'Operations', 'System', 'Governance'];

  itemsFor(section: string) {
    return this.nav.items.filter((i) => i.section === section);
  }

  iconGlyph(key: string): string {
    const map: Record<string, string> = {
      dashboard: '▣',
      users: '👤',
      media: '▶',
      jobs: '⟳',
      platforms: '◈',
      providers: '⬡',
      storage: '▤',
      reports: '▦',
      health: '♥',
      logs: '≡',
      admins: '★',
      roles: '⚿',
      audit: '✎',
      settings: '⚙',
    };
    return map[key] ?? '•';
  }
}
