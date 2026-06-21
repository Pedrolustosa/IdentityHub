import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-skeleton-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
      <!-- Header skeleton -->
      @if (showHeader) {
        <div class="mb-4 flex items-center justify-between gap-4">
          <div class="h-6 w-48 animate-pulse rounded bg-slate-100"></div>
          <div class="h-5 w-20 animate-pulse rounded bg-slate-100"></div>
        </div>
      }

      <!-- Content skeleton -->
      <div class="space-y-3">
        @for (i of [1, 2, 3]; track i) {
          <div class="space-y-2">
            <div class="h-4 w-1/3 animate-pulse rounded bg-slate-100"></div>
            <div class="h-3 w-full animate-pulse rounded bg-slate-50"></div>
            <div class="h-3 w-5/6 animate-pulse rounded bg-slate-50"></div>
          </div>
        }
      </div>

      <!-- Footer skeleton -->
      @if (showFooter) {
        <div class="mt-6 flex gap-3">
          <div class="h-10 w-24 animate-pulse rounded-lg bg-slate-100"></div>
          <div class="h-10 w-24 animate-pulse rounded-lg bg-slate-100"></div>
        </div>
      }
    </div>
  `
})
export class SkeletonCardComponent {
  @Input() showHeader = true;
  @Input() showFooter = true;
}
