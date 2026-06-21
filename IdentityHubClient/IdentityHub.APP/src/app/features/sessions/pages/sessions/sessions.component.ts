import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastrService } from 'ngx-toastr';
import { UsersService, UserListItem } from '../../../users/users.service';
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [CommonModule, LoadErrorBannerComponent],
  template: `
    <section class="space-y-5">
      <header class="rounded-2xl border border-slate-200/80 bg-white px-5 py-4 shadow-sm">
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">System sessions</h1>
        <p class="text-slate-600">Admin-level view of user sessions across the system.</p>
      </header>

      <div class="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
        Detailed session metadata (IP, browser, OS, revoke by session) depends on backend endpoint <span class="font-mono">GET /api/sessions</span>.
      </div>

      @if (isLoading) {
        <div class="rounded-xl border bg-white p-6 shadow-sm">
          <div class="h-8 w-64 animate-pulse rounded bg-slate-100"></div>
        </div>
      } @else if (loadError) {
        <div class="rounded-xl border bg-white p-4 shadow-sm">
          <app-load-error-banner [error]="loadError" (retry)="load()" />
        </div>
      } @else {
        <div class="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm">
          <table class="w-full min-w-[980px] text-sm">
            <thead>
              <tr class="bg-slate-50 text-slate-500 uppercase text-xs tracking-wide">
                <th class="text-left px-3 py-3 font-semibold">User</th>
                <th class="text-left px-3 py-3 font-semibold">Email</th>
                <th class="text-left px-3 py-3 font-semibold">IP</th>
                <th class="text-left px-3 py-3 font-semibold">Browser</th>
                <th class="text-left px-3 py-3 font-semibold">OS</th>
                <th class="text-left px-3 py-3 font-semibold">Status</th>
                <th class="text-right px-3 py-3 font-semibold">Action</th>
              </tr>
            </thead>
            <tbody>
              @for (user of users; track user.id) {
                <tr class="border-t border-slate-100">
                  <td class="px-3 py-2 text-slate-800">{{ user.fullName || 'Not provided' }}</td>
                  <td class="px-3 py-2 text-slate-700">{{ user.email || '-' }}</td>
                  <td class="px-3 py-2 text-slate-400">-</td>
                  <td class="px-3 py-2 text-slate-400">-</td>
                  <td class="px-3 py-2 text-slate-400">-</td>
                  <td class="px-3 py-2">
                    <span class="rounded-full px-2 py-0.5 text-xs font-semibold"
                      [ngClass]="user.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-600'">
                      {{ user.isActive ? 'Active' : 'Inactive' }}
                    </span>
                  </td>
                  <td class="px-3 py-2 text-right">
                    <button type="button" disabled class="rounded-lg border border-slate-300 px-2.5 py-1 text-xs text-slate-500 opacity-70">Revoke session</button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `
})
export class SessionsComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  users: UserListItem[] = [];

  constructor(
    private readonly usersService: UsersService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = null;

    this.usersService.getUsers().pipe(finalize(() => (this.isLoading = false))).subscribe({
      next: (users) => {
        this.users = users;
      },
      error: (err: unknown) => {
        const mapped = mapHttpToUiLoadError(err);
        this.loadError = mapped;
        this.toastr.error(toastMessageForUiLoadError(mapped), 'System sessions');
      }
    });
  }
}
