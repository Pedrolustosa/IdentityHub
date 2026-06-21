import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-security-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <section class="space-y-5">
      <header class="rounded-2xl border border-slate-200/80 bg-white px-5 py-4 shadow-sm">
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">Security settings</h1>
        <p class="text-slate-600">Centralized, configurable security policies.</p>
      </header>

      <form [formGroup]="form" (ngSubmit)="save()" class="rounded-xl border border-slate-200 bg-white p-5 shadow-sm grid grid-cols-1 gap-4 md:grid-cols-2">
        <label class="space-y-1 text-sm text-slate-600">
          <span class="font-medium text-slate-700">Access token (minutes)</span>
          <input type="number" formControlName="accessTokenMinutes" class="w-full rounded-lg border border-slate-300 px-3 py-2" />
        </label>
        <label class="space-y-1 text-sm text-slate-600">
          <span class="font-medium text-slate-700">Refresh token (days)</span>
          <input type="number" formControlName="refreshTokenDays" class="w-full rounded-lg border border-slate-300 px-3 py-2" />
        </label>
        <label class="space-y-1 text-sm text-slate-600">
          <span class="font-medium text-slate-700">Login attempts before lock</span>
          <input type="number" formControlName="maxLoginAttempts" class="w-full rounded-lg border border-slate-300 px-3 py-2" />
        </label>
        <label class="space-y-1 text-sm text-slate-600">
          <span class="font-medium text-slate-700">Lock duration (minutes)</span>
          <input type="number" formControlName="lockDurationMinutes" class="w-full rounded-lg border border-slate-300 px-3 py-2" />
        </label>
        <label class="md:col-span-2 inline-flex items-center gap-2 text-sm text-slate-700">
          <input type="checkbox" formControlName="requireEmailConfirmation" />
          Require e-mail confirmation
        </label>
        <div class="md:col-span-2 border-t border-slate-100 pt-3">
          <button type="submit" [disabled]="form.invalid" class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-500 disabled:opacity-60">
            Save settings
          </button>
        </div>
      </form>
    </section>
  `
})
export class SecuritySettingsComponent {
  readonly form = new FormBuilder().nonNullable.group({
    accessTokenMinutes: [30, [Validators.required, Validators.min(5)]],
    refreshTokenDays: [7, [Validators.required, Validators.min(1)]],
    maxLoginAttempts: [5, [Validators.required, Validators.min(1)]],
    lockDurationMinutes: [15, [Validators.required, Validators.min(1)]],
    requireEmailConfirmation: [true]
  });

  constructor(private readonly toastr: ToastrService) {}

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.toastr.success('Security settings saved (UI scaffold).', 'Security settings');
  }
}
