import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastrService } from 'ngx-toastr';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../../core/services/auth.service';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UserInvitesService, UserInviteItem } from '../../user-invites.service';

type InviteStatus = 'Pending' | 'Accepted' | 'Expired' | 'Canceled';

@Component({
  selector: 'app-user-invites',
  standalone: true,
  imports: [CommonModule, UxStateComponent],
  template: `
    <section class="space-y-5">
      <header class="rounded-2xl border border-slate-200/80 bg-white px-5 py-4 shadow-sm">
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">User invites</h1>
        <p class="text-slate-600">Track pending, accepted, expired and canceled invites.</p>
      </header>

      <app-ux-state
        [state]="isLoading ? 'loading' : loadError ? 'error' : invites.length === 0 ? 'empty' : 'loaded'"
        [error]="loadError"
        title="No invites"
        description="No user invites have been sent yet."
        (retry)="load()"
      >
        <div class="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm">
          <table class="w-full min-w-[900px] text-sm">
            <thead>
              <tr class="bg-slate-50 text-slate-500 uppercase text-xs tracking-wide">
                <th class="text-left px-3 py-3 font-semibold">Email</th>
                <th class="text-left px-3 py-3 font-semibold">Name</th>
                <th class="text-left px-3 py-3 font-semibold">Initial role</th>
                <th class="text-left px-3 py-3 font-semibold">Status</th>
                <th class="text-left px-3 py-3 font-semibold">Sent at</th>
                <th class="text-right px-3 py-3 font-semibold">Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (invite of invites; track invite.id) {
                <tr class="border-t border-slate-100">
                  <td class="px-3 py-2 text-slate-800">{{ invite.email }}</td>
                  <td class="px-3 py-2 text-slate-700">{{ invite.fullName }}</td>
                  <td class="px-3 py-2 text-slate-700">{{ invite.role }}</td>
                  <td class="px-3 py-2">
                    <span class="rounded-full px-2 py-0.5 text-xs font-semibold"
                      [ngClass]="statusClass(invite.status)">{{ invite.status }}</span>
                  </td>
                  <td class="px-3 py-2 text-slate-700">{{ invite.sentAt | date:'short' }}</td>
                  <td class="px-3 py-2 text-right">
                    <div class="inline-flex gap-1">
                      <button type="button" (click)="resendInvite(invite)" [disabled]="!canResendInvites || isProcessing" class="rounded border border-slate-300 px-2 py-1 text-xs text-slate-700 disabled:opacity-60">Resend</button>
                      <button type="button" (click)="cancelInvite(invite)" [disabled]="!canCancelInvites || isProcessing" class="rounded border border-rose-200 bg-rose-50 px-2 py-1 text-xs text-rose-700 disabled:opacity-60">Cancel</button>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </app-ux-state>
    </section>
  `
})
export class UserInvitesComponent implements OnInit {
  readonly canResendInvites: boolean;
  readonly canCancelInvites: boolean;

  isLoading = true;
  isProcessing = false;
  loadError: UiLoadError | null = null;
  invites: UserInviteItem[] = [];

  constructor(
    private readonly authService: AuthService,
    @Inject(UserInvitesService) private readonly userInvitesService: UserInvitesService,
    private readonly toastr: ToastrService
  ) {
    this.canResendInvites = this.authService.hasPermission('UserInvites.Resend');
    this.canCancelInvites = this.authService.hasPermission('UserInvites.Cancel');
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = null;
    this.invites = [];

    this.userInvitesService
      .getUserInvites()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.invites = response.items;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'User invites');
        }
      });
  }

  resendInvite(invite: UserInviteItem): void {
    if (!this.canResendInvites) {
      this.toastr.warning('You do not have permission to resend invites.', 'User invites');
      return;
    }

    if (this.isProcessing) {
      return;
    }

    this.isProcessing = true;

    this.userInvitesService
      .resendInvite(invite.id)
      .pipe(finalize(() => (this.isProcessing = false)))
      .subscribe({
        next: () => {
          this.toastr.success(`Invite resent to ${invite.email}`, 'User invites');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'User invites');
        }
      });
  }

  cancelInvite(invite: UserInviteItem): void {
    if (!this.canCancelInvites) {
      this.toastr.warning('You do not have permission to cancel invites.', 'User invites');
      return;
    }

    if (this.isProcessing) {
      return;
    }

    if (!confirm(`Are you sure you want to cancel the invite for ${invite.email}?`)) {
      return;
    }

    this.isProcessing = true;

    this.userInvitesService
      .cancelInvite(invite.id)
      .pipe(finalize(() => (this.isProcessing = false)))
      .subscribe({
        next: () => {
          this.invites = this.invites.filter((i) => i.id !== invite.id);
          this.toastr.success(`Invite canceled for ${invite.email}`, 'User invites');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'User invites');
        }
      });
  }

  statusClass(status: InviteStatus): string {
    switch (status) {
      case 'Accepted':
        return 'bg-emerald-100 text-emerald-700';
      case 'Pending':
        return 'bg-amber-100 text-amber-700';
      case 'Expired':
        return 'bg-slate-100 text-slate-700';
      case 'Canceled':
        return 'bg-rose-100 text-rose-700';
      default:
        return 'bg-slate-100 text-slate-700';
    }
  }
}
