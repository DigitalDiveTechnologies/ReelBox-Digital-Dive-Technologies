import { Injectable, signal } from '@angular/core';

export type AdminTheme = 'light' | 'dark';

const STORAGE_KEY = 'reelbox-admin-theme';

/**
 * Default: light (Sir / Prime Tech). Dark is on-demand via toggle.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly _theme = signal<AdminTheme>(this.readInitial());

  readonly theme = this._theme.asReadonly();

  constructor() {
    this.apply(this._theme());
  }

  isDark(): boolean {
    return this._theme() === 'dark';
  }

  setTheme(theme: AdminTheme): void {
    this._theme.set(theme);
    this.apply(theme);
    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {
      /* ignore quota / private mode */
    }
  }

  toggle(): void {
    this.setTheme(this.isDark() ? 'light' : 'dark');
  }

  private readInitial(): AdminTheme {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'dark' || stored === 'light') {
        return stored;
      }
    } catch {
      /* ignore */
    }
    return 'light';
  }

  private apply(theme: AdminTheme): void {
    if (typeof document === 'undefined') {
      return;
    }
    document.documentElement.setAttribute('data-theme', theme);
  }
}
