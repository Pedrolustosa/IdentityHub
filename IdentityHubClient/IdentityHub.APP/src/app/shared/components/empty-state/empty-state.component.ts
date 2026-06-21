import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="flex flex-col items-center justify-center rounded-xl border border-dashed border-muted-lighter bg-muted-lightest px-6 py-12 text-center">
      @if (icon) {
        <div class="mb-4 text-4xl">{{ icon }}</div>
      }
      <h3 class="text-lg font-semibold text-slate-900">{{ title }}</h3>
      @if (description) {
        <p class="mt-1 text-sm text-muted">{{ description }}</p>
      }
      @if (actionLabel && actionRoute) {
        <a [routerLink]="actionRoute" class="mt-4 inline-flex items-center rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700">
          {{ actionLabel }}
        </a>
      }
    </div>
  `
})
export class EmptyStateComponent {
  @Input() icon = '📭';
  @Input({ required: true }) title!: string;
  @Input() description: string | null = null;
  @Input() actionLabel: string | null = null;
  @Input() actionRoute: string | string[] | null = null;
}
