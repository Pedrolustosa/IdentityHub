/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        /* Semantic surfaces / text — driven by CSS vars in styles.css */
        canvas: 'rgb(var(--ih-canvas) / <alpha-value>)',
        'canvas-subtle': 'rgb(var(--ih-canvas-subtle) / <alpha-value>)',
        panel: 'rgb(var(--ih-panel) / <alpha-value>)',
        'panel-muted': 'rgb(var(--ih-panel-muted) / <alpha-value>)',
        elevated: 'rgb(var(--ih-elevated) / <alpha-value>)',
        ink: 'rgb(var(--ih-ink) / <alpha-value>)',
        'ink-secondary': 'rgb(var(--ih-ink-secondary) / <alpha-value>)',
        soft: 'rgb(var(--ih-ink-muted) / <alpha-value>)',
        line: 'rgb(var(--ih-line) / <alpha-value>)',
        'line-strong': 'rgb(var(--ih-line-strong) / <alpha-value>)',
        rail: 'rgb(var(--ih-rail) / <alpha-value>)',
        'rail-muted': 'rgb(var(--ih-rail-muted) / <alpha-value>)',
        'rail-hover': 'rgb(var(--ih-rail-hover) / <alpha-value>)',
        'rail-active': 'rgb(var(--ih-rail-active) / <alpha-value>)',
        'rail-ink': 'rgb(var(--ih-rail-ink) / <alpha-value>)',
        'rail-soft': 'rgb(var(--ih-rail-soft) / <alpha-value>)',
        sidebar: 'rgb(var(--ih-sidebar) / <alpha-value>)',
        'sidebar-muted': 'rgb(var(--ih-sidebar-muted) / <alpha-value>)',
        'sidebar-hover': 'rgb(var(--ih-sidebar-hover) / <alpha-value>)',
        'sidebar-active': 'rgb(var(--ih-sidebar-active) / <alpha-value>)',
        'sidebar-ink': 'rgb(var(--ih-sidebar-ink) / <alpha-value>)',
        'sidebar-soft': 'rgb(var(--ih-sidebar-soft) / <alpha-value>)',
        'sidebar-line': 'rgb(var(--ih-sidebar-line) / <alpha-value>)',
        'sidebar-accent': 'rgb(var(--ih-sidebar-accent) / <alpha-value>)',

        /* Brand accent — cool trust blue (IAM), not purple */
        primary: {
          50: '#f0f7fb',
          100: '#d9ebf5',
          200: '#b3d7eb',
          300: '#7cb8d9',
          400: '#3d94c0',
          500: '#1a7aad',
          600: '#15628f',
          700: '#124e73',
          800: '#113f5c',
          900: '#0f354d'
        },
        danger: {
          50: '#fef2f2',
          100: '#fee2e2',
          200: '#fecaca',
          300: '#fca5a5',
          400: '#f87171',
          500: '#ef4444',
          600: '#dc2626',
          700: '#b91c1c',
          800: '#991b1b',
          900: '#7f1d1d'
        },
        warning: {
          50: '#fffbeb',
          100: '#fef3c7',
          200: '#fde68a',
          300: '#fcd34d',
          400: '#fbbf24',
          500: '#f59e0b',
          600: '#d97706',
          700: '#b45309',
          800: '#92400e',
          900: '#78350f'
        },
        success: {
          50: '#f0fdf6',
          100: '#dcfce9',
          200: '#bbf7d4',
          300: '#86efb0',
          400: '#4ade80',
          500: '#22c55e',
          600: '#16a34a',
          700: '#15803d',
          800: '#166534',
          900: '#14532d'
        },
        surface: {
          50: '#f8fafc',
          100: '#f1f5f9',
          200: '#e2e8f0',
          300: '#cbd5e1',
          400: '#94a3b8',
          500: '#64748b',
          600: '#475569',
          700: '#334155',
          800: '#1e293b',
          900: '#0f172a'
        },
        muted: {
          DEFAULT: '#64748b',
          light: '#94a3b8',
          lighter: '#cbd5e1',
          lightest: '#f1f5f9'
        }
      },
      boxShadow: {
        panel: 'var(--ih-shadow-panel)',
        lift: 'var(--ih-shadow-lift)'
      },
      spacing: {
        xs: '0.25rem',
        sm: '0.5rem',
        md: '1rem',
        lg: '1.5rem',
        xl: '2rem',
        '2xl': '3rem'
      },
      fontSize: {
        xs: ['0.75rem', { lineHeight: '1rem' }],
        sm: ['0.875rem', { lineHeight: '1.25rem' }],
        base: ['1rem', { lineHeight: '1.5rem' }],
        lg: ['1.125rem', { lineHeight: '1.75rem' }],
        xl: ['1.25rem', { lineHeight: '1.75rem' }],
        '2xl': ['1.5rem', { lineHeight: '2rem' }]
      }
    }
  },
  plugins: []
};
