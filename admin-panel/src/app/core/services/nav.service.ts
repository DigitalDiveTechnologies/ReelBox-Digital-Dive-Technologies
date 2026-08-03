import { Injectable, signal } from '@angular/core';
import { NavItem } from '../models/nav-item.model';

/** PDF §6.1 sidebar / route map — UI navigation only. */
@Injectable({ providedIn: 'root' })
export class NavService {
  readonly sidebarCollapsed = signal(false);
  /** Mobile drawer visibility (≤900px). */
  readonly mobileOpen = signal(false);

  readonly items: NavItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard', section: 'Overview' },
    { label: 'Users', route: '/users', icon: 'users', section: 'Operations' },
    { label: 'Media', route: '/media', icon: 'media', section: 'Operations' },
    { label: 'Download Jobs', route: '/jobs', icon: 'jobs', section: 'Operations' },
    { label: 'Platforms', route: '/platforms', icon: 'platforms', section: 'Operations' },
    { label: 'Providers', route: '/providers', icon: 'providers', section: 'System' },
    { label: 'Storage', route: '/storage', icon: 'storage', section: 'System' },
    { label: 'Reports', route: '/reports', icon: 'reports', section: 'System' },
    { label: 'System Health', route: '/system-health', icon: 'health', section: 'System' },
    { label: 'Logs', route: '/logs', icon: 'logs', section: 'System' },
    { label: 'Admin Users', route: '/admin-users', icon: 'admins', section: 'Governance' },
    { label: 'Roles', route: '/roles', icon: 'roles', section: 'Governance' },
    { label: 'Audit Logs', route: '/audit-logs', icon: 'audit', section: 'Governance' },
    { label: 'Settings', route: '/settings', icon: 'settings', section: 'Governance' },
  ];

  toggleSidebar(): void {
    if (typeof window !== 'undefined' && window.matchMedia('(max-width: 900px)').matches) {
      this.mobileOpen.update((v) => !v);
      return;
    }
    this.sidebarCollapsed.update((v) => !v);
  }

  closeMobileNav(): void {
    this.mobileOpen.set(false);
  }
}
