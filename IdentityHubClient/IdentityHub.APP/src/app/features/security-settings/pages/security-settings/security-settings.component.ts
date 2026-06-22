import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { finalize } from 'rxjs/operators';
import { AuthService } from '../../../../core/services/auth.service';
import { SecuritySettingsService, SecuritySettings } from '../../security-settings.service';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';

@Component({
  selector: 'app-security-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, UxStateComponent],
  template: `
    <section class="space-y-5">
      <header class="rounded-2xl border border-slate-200/80 bg-white px-5 py-4 shadow-sm">
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">Security settings</h1>
        <p class="text-slate-600">Centralized, configurable security policies.</p>
      </header>

      <app-ux-state
        [state]="isLoading ? 'loading' : loadError ? 'error' : 'loaded'"
        [error]="loadError"
        title="Unable to load"
        description="Could not retrieve security settings."
        (retry)="load()"
      >
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
            <button type="submit" [disabled]="form.invalid || !canUpdateSettings || isSaving" class="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-500 disabled:opacity-60">
              {{ isSaving ? 'Saving...' : 'Save settings' }}
            </button>
            @if (!canUpdateSettings) {
              <p class="mt-2 text-sm text-slate-600">You have read-only access to security settings.</p>
            }
          </div>
        </form>
      </app-ux-state>
    </section>
  `
})
export class SecuritySettingsComponent implements OnInit {
  readonly form = new FormBuilder().nonNullable.group({
    accessTokenMinutes: [30, [Validators.required, Validators.min(5)]],
    refreshTokenDays: [7, [Validators.required, Validators.min(1)]],
    maxLoginAttempts: [5, [Validators.required, Validators.min(1)]],
    lockDurationMinutes: [15, [Validators.required, Validators.min(1)]],
    requireEmailConfirmation: [true]
  });

  readonly canUpdateSettings: boolean;
  isLoading = true;
  isSaving = false;
  loadError: UiLoadError | null = null;

  constructor(
    private readonly toastr: ToastrService,
    private readonly authService: AuthService,
    @Inject(SecuritySettingsService) private readonly securitySettingsService: SecuritySettingsService
  ) {
    this.canUpdateSettings = this.authService.hasPermission('SecuritySettings.Update');
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = null;

    this.securitySettingsService
      .getSettings()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (settings) => {
          this.form.patchValue({
            accessTokenMinutes: settings.accessTokenMinutes,
            refreshTokenDays: settings.refreshTokenDays,
            maxLoginAttempts: settings.maxLoginAttempts,
            lockDurationMinutes: settings.lockDurationMinutes,
            requireEmailConfirmation: settings.requireEmailConfirmation
          });
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Security settings');
        }
      });
  }

  save(): void {
    if (!this.canUpdateSettings) {
      this.toastr.warning('You do not have permission to update settings.', 'Security settings');
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.isSaving) {
      return;
    }

    this.isSaving = true;
    const settings = this.form.getRawValue() as SecuritySettings;

    this.securitySettingsService
      .updateSettings(settings)
      .pipe(finalize(() => (this.isSaving = false)))
      .subscribe({
        next: () => {
          this.toastr.success('Security settings updated.', 'Security settings');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Security settings');
        }
      });
  }
}
