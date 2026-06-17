import { Injectable, signal } from '@angular/core';

export type AppTheme = 'light' | 'dark';

const STORAGE_KEY = 'ih-theme';

/**
 * Light/dark theme manager. Toggles the `dark` class on the root element
 * (Tailwind `darkMode: 'class'`) and persists the choice in localStorage.
 * SSR-safe: all DOM/storage access is guarded.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme = signal<AppTheme>('light');

  init(): void {
    if (typeof window === 'undefined') {
      return;
    }

    const stored = localStorage.getItem(STORAGE_KEY) as AppTheme | null;
    const prefersDark =
      typeof window.matchMedia === 'function' && window.matchMedia('(prefers-color-scheme: dark)').matches;
    const initial: AppTheme = stored ?? (prefersDark ? 'dark' : 'light');

    this.apply(initial);
  }

  toggle(): void {
    this.apply(this.theme() === 'dark' ? 'light' : 'dark');
  }

  private apply(theme: AppTheme): void {
    this.theme.set(theme);

    if (typeof document === 'undefined') {
      return;
    }

    document.documentElement.classList.toggle('dark', theme === 'dark');

    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(STORAGE_KEY, theme);
    }
  }
}
