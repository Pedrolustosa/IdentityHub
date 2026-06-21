import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="mx-auto max-w-3xl space-y-6">
      <header class="rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 shadow-sm">
        <h1 class="text-2xl font-semibold tracking-tight text-rose-900">Access denied</h1>
        <p class="mt-1 text-rose-700">You do not have permission to access this page.</p>
      </header>

      <div class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm space-y-4">
        <div>
          <p class="text-xs font-semibold uppercase tracking-wide text-slate-500">Required permissions</p>
          @if (requiredPermissions.length === 0) {
            <p class="mt-2 text-sm text-slate-500">No specific permission was provided.</p>
          } @else {
            <ul class="mt-2 flex flex-wrap gap-2">
              @for (perm of requiredPermissions; track perm) {
                <li class="rounded-full border border-rose-200 bg-rose-50 px-2.5 py-1 text-xs font-mono text-rose-700">{{ perm }}</li>
              }
            </ul>
          }
        </div>

        <div>
          <p class="text-xs font-semibold uppercase tracking-wide text-slate-500">Your permissions</p>
          @if (myPermissions.length === 0) {
            <p class="mt-2 text-sm text-slate-500">No effective permissions available.</p>
          } @else {
            <ul class="mt-2 flex flex-wrap gap-2">
              @for (perm of myPermissions; track perm) {
                <li class="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-xs font-mono text-slate-700">{{ perm }}</li>
              }
            </ul>
          }
        </div>

        <div class="flex flex-wrap gap-2 border-t border-slate-100 pt-3">
          <a routerLink="/app/dashboard" class="rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white hover:bg-blue-500">Go to dashboard</a>
          <a routerLink="/app/my-access" class="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50">Open my access</a>
        </div>
      </div>
    </section>
  `
})
export class AccessDeniedComponent {
  readonly requiredPermissions: string[];
  readonly myPermissions: string[];

  constructor(route: ActivatedRoute, authService: AuthService) {
    const requiredRaw = route.snapshot.queryParamMap.get('required') ?? '';
    this.requiredPermissions = requiredRaw
      .split(',')
      .map((entry) => entry.trim())
      .filter((entry) => entry.length > 0);
    this.myPermissions = authService.getApplicationPermissions();
  }
}
