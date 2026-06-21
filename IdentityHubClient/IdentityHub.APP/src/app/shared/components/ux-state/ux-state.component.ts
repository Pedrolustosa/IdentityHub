import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LoadErrorBannerComponent } from '../load-error-banner/load-error-banner.component';
import { UiLoadError } from '../../http/ui-load-error';

export type UxStateKind = 'loaded' | 'loading' | 'error' | 'empty' | 'forbidden' | 'success' | 'in-progress';

@Component({
  selector: 'app-ux-state',
  standalone: true,
  imports: [RouterLink, LoadErrorBannerComponent],
  template: `
    @switch (state) {
      @case ('loaded') {
        <ng-content />
      }
      @case ('loading') {
        <div class="rounded-xl border border-slate-200/80 bg-white p-5 shadow-sm space-y-4" aria-busy="true">
          <div class="h-8 w-56 rounded-md bg-slate-100 animate-pulse"></div>
          <div class="space-y-3">
            <div class="h-4 w-3/4 rounded bg-slate-100 animate-pulse"></div>
            <div class="h-4 w-2/3 rounded bg-slate-100 animate-pulse"></div>
            <div class="h-24 rounded-lg bg-slate-100 animate-pulse"></div>
          </div>
          @if (description.trim()) {
            <p class="text-sm text-slate-500">{{ description }}</p>
          }
        </div>
      }
      @case ('error') {
        <app-load-error-banner
          [error]="error ?? { kind: 'unknown' }"
          [notFoundBackLink]="notFoundBackLink"
          [notFoundBackLabel]="notFoundBackLabel"
          (retry)="retry.emit()"
        />
      }
      @case ('empty') {
        <div class="rounded-xl border border-slate-200/80 bg-white p-6 text-center shadow-sm space-y-4">
          <div class="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-slate-100 text-slate-500">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 8v8m4-4H8" />
              <path stroke-linecap="round" stroke-linejoin="round" d="M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
            </svg>
          </div>
          <div class="space-y-2">
            <h2 class="text-base font-semibold text-slate-900">{{ title }}</h2>
            <p class="text-sm leading-relaxed text-slate-600">{{ description }}</p>
          </div>
          @if (actionLabel.trim()) {
            @if (actionRoute) {
              <a
                [routerLink]="actionRoute"
                class="inline-flex items-center justify-center rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white transition-colors duration-150 hover:bg-blue-500"
              >
                {{ actionLabel }}
              </a>
            } @else {
              <button
                type="button"
                (click)="action.emit()"
                class="inline-flex items-center justify-center rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white transition-colors duration-150 hover:bg-blue-500"
              >
                {{ actionLabel }}
              </button>
            }
          }
        </div>
      }
      @case ('forbidden') {
        <div class="rounded-xl border border-amber-200 bg-amber-50 p-6 text-center shadow-sm space-y-4">
          <div class="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-amber-100 text-amber-700">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path stroke-linecap="round" stroke-linejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 1 0-9 0v3.75M5.25 10.5h13.5l-1.114 9.003A1.5 1.5 0 0 1 16.529 21H7.47a1.5 1.5 0 0 1-1.49-1.497L5.25 10.5Z" />
            </svg>
          </div>
          <div class="space-y-2">
            <h2 class="text-base font-semibold text-amber-950">{{ title }}</h2>
            <p class="text-sm leading-relaxed text-amber-900/90">{{ description }}</p>
          </div>
          @if (actionLabel.trim()) {
            @if (actionRoute) {
              <a
                [routerLink]="actionRoute"
                class="inline-flex items-center justify-center rounded-lg bg-amber-600 px-4 py-2 text-sm font-medium text-white transition-colors duration-150 hover:bg-amber-500"
              >
                {{ actionLabel }}
              </a>
            } @else {
              <button
                type="button"
                (click)="action.emit()"
                class="inline-flex items-center justify-center rounded-lg bg-amber-600 px-4 py-2 text-sm font-medium text-white transition-colors duration-150 hover:bg-amber-500"
              >
                {{ actionLabel }}
              </button>
            }
          }
        </div>
      }
      @case ('success') {
        <div class="rounded-xl border border-emerald-200 bg-emerald-50 p-6 text-center shadow-sm space-y-4">
          <div class="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-emerald-100 text-emerald-700">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path stroke-linecap="round" stroke-linejoin="round" d="m9 12.75 2.25 2.25L15 9.75" />
              <path stroke-linecap="round" stroke-linejoin="round" d="M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
            </svg>
          </div>
          <div class="space-y-2">
            <h2 class="text-base font-semibold text-emerald-950">{{ title }}</h2>
            <p class="text-sm leading-relaxed text-emerald-900/90">{{ description }}</p>
          </div>
          @if (actionLabel.trim()) {
            @if (actionRoute) {
              <a
                [routerLink]="actionRoute"
                class="inline-flex items-center justify-center rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white transition-colors duration-150 hover:bg-emerald-500"
              >
                {{ actionLabel }}
              </a>
            } @else {
              <button
                type="button"
                (click)="action.emit()"
                class="inline-flex items-center justify-center rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white transition-colors duration-150 hover:bg-emerald-500"
              >
                {{ actionLabel }}
              </button>
            }
          }
        </div>
      }
      @case ('in-progress') {
        <div class="rounded-xl border border-sky-200 bg-sky-50 p-6 text-center shadow-sm space-y-4" aria-busy="true">
          <div class="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-sky-100 text-sky-700">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6 animate-spin" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 3v3m6.364.636-2.121 2.121M21 12h-3m-.636 6.364-2.121-2.121M12 21v-3m-6.364-.636 2.121-2.121M3 12h3m.636-6.364 2.121 2.121" />
            </svg>
          </div>
          <div class="space-y-2">
            <h2 class="text-base font-semibold text-sky-950">{{ busyLabel }}</h2>
            @if (description.trim()) {
              <p class="text-sm leading-relaxed text-sky-900/90">{{ description }}</p>
            }
          </div>
        </div>
      }
    }
  `
})
export class UxStateComponent {
  @Input({ required: true }) state!: UxStateKind;
  @Input() error: UiLoadError | null = null;
  @Input() title = '';
  @Input() description = '';
  @Input() busyLabel = 'Working on this request…';
  @Input() actionLabel = '';
  @Input() actionRoute: string | any[] | null = null;
  @Input() notFoundBackLink: string | any[] | null = null;
  @Input() notFoundBackLabel = 'Go back';

  @Output() readonly retry = new EventEmitter<void>();
  @Output() readonly action = new EventEmitter<void>();
}
