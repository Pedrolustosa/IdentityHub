import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

type InviteStatus = 'Pending' | 'Accepted' | 'Expired' | 'Canceled';
type InviteRow = {
  id: string;
  email: string;
  fullName: string;
  role: string;
  status: InviteStatus;
  sentAt: string;
};

@Component({
  selector: 'app-user-invites',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="space-y-5">
      <header class="rounded-2xl border border-slate-200/80 bg-white px-5 py-4 shadow-sm">
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">User invites</h1>
        <p class="text-slate-600">Track pending, accepted, expired and canceled invites.</p>
      </header>

      <div class="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
        Temporary UI scaffold. Connect this screen to a dedicated invites API to persist data and actions.
      </div>

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
                    <button type="button" class="rounded border border-slate-300 px-2 py-1 text-xs text-slate-700">Resend</button>
                    <button type="button" class="rounded border border-rose-200 bg-rose-50 px-2 py-1 text-xs text-rose-700">Cancel</button>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>
  `
})
export class UserInvitesComponent {
  readonly invites: InviteRow[] = [
    {
      id: '1',
      email: 'manager@company.com',
      fullName: 'Manager User',
      role: 'Manager',
      status: 'Pending',
      sentAt: new Date().toISOString()
    }
  ];

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
