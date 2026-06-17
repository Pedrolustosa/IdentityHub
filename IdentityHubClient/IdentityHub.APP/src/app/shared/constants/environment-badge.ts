import { environment } from '../../../environments/environment';

export interface EnvironmentBadge {
  label: string;
  /** Tailwind classes for the badge chip. */
  classes: string;
  /** Whether the badge should be displayed at all. */
  visible: boolean;
}

const BADGES: Record<string, EnvironmentBadge> = {
  development: {
    label: 'DEV',
    classes: 'bg-emerald-100 text-emerald-700 ring-emerald-200 dark:bg-emerald-500/15 dark:text-emerald-300 dark:ring-emerald-500/30',
    visible: true
  },
  staging: {
    label: 'HOMOLOG',
    classes: 'bg-amber-100 text-amber-800 ring-amber-200 dark:bg-amber-500/15 dark:text-amber-300 dark:ring-amber-500/30',
    visible: true
  },
  production: {
    label: 'PROD',
    classes: 'bg-rose-100 text-rose-700 ring-rose-200 dark:bg-rose-500/15 dark:text-rose-300 dark:ring-rose-500/30',
    visible: false
  }
};

export function getEnvironmentBadge(): EnvironmentBadge {
  const name = (environment as { name?: string }).name ?? (environment.production ? 'production' : 'development');
  return BADGES[name] ?? BADGES['production'];
}
