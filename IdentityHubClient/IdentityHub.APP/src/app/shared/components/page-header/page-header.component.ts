import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

export interface BreadcrumbItem {
  label: string;
  route?: string | string[];
}

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="ih-chrome border-b border-line bg-panel px-6 py-4 sm:px-8">
      <!-- Breadcrumbs -->
      @if (breadcrumbs && breadcrumbs.length > 0) {
        <nav class="mb-3 flex items-center gap-2 text-sm text-soft">
          @for (item of breadcrumbs; track item.label; let last = $last) {
            @if (!last) {
              @if (item.route) {
                <a [routerLink]="item.route" class="ih-link">{{ item.label }}</a>
              } @else {
                <span class="text-ink-secondary">{{ item.label }}</span>
              }
              <svg class="h-4 w-4 text-soft" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
            } @else {
              <span class="font-medium text-ink">{{ item.label }}</span>
            }
          }
        </nav>
      }

      <!-- Title and actions -->
      <div class="flex items-start justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-ink">{{ title }}</h1>
          @if (subtitle) {
            <p class="mt-1 text-sm text-soft">{{ subtitle }}</p>
          }
        </div>
        @if (actionLabel && actionRoute) {
          <a [routerLink]="actionRoute" class="btn btn-primary btn-md mt-1 whitespace-nowrap">
            @if (actionIcon) {
              <span class="mr-2">{{ actionIcon }}</span>
            }
            {{ actionLabel }}
          </a>
        }
      </div>
    </div>
  `
})
export class PageHeaderComponent {
  @Input() title = 'Page Title';
  @Input() subtitle: string | null = null;
  @Input() breadcrumbs: BreadcrumbItem[] | null = null;
  @Input() actionLabel: string | null = null;
  @Input() actionRoute: string | string[] | null = null;
  @Input() actionIcon: string | null = null;
}
