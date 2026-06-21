import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { CriticalActionConfirmationService } from '../../../../shared/services/critical-action-confirmation.service';
import { EMPTY, catchError, finalize, map, switchMap } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService, MeResponse, ProfileResponse, UserSessionResponse } from '../../../../core/services/auth.service';

function profileUpdateErrorMessage(err: unknown): string {
  if (!(err instanceof HttpErrorResponse)) {
    return 'Could not update profile.';
  }

  const body = err.error;

  if (typeof body === 'string' && body.trim()) {
    return body;
  }

  if (Array.isArray(body) && body.length > 0) {
    const first = body[0] as { description?: string; Description?: string };
    const msg = first.description ?? first.Description;
    if (typeof msg === 'string' && msg.trim()) {
      return msg;
    }
  }

  return 'Could not update profile.';
}

/** Non-alphanumeric, excluding whitespace (symbol). */
function hasPasswordSpecialChar(value: string): boolean {
  return /[^A-Za-z0-9\s]/.test(value);
}

function profilePasswordStrengthValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = (control.value ?? '') as string;
    if (!value) {
      return null;
    }
    const errors: Record<string, true> = {};
    if (value.length < 7 || value.length > 12) {
      errors['profilePasswordLength'] = true;
    }
    if (!/[A-Z]/.test(value)) {
      errors['profilePasswordUppercase'] = true;
    }
    if ((value.match(/\d/g) ?? []).length < 2) {
      errors['profilePasswordTwoDigits'] = true;
    }
    if (!hasPasswordSpecialChar(value)) {
      errors['profilePasswordSpecial'] = true;
    }
    return Object.keys(errors).length ? { profilePasswordStrength: errors } : null;
  };
}

function profilePasswordMatchValidator(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const g = group as FormGroup;
    const np = g.get('newPassword')?.value ?? '';
    const cp = g.get('confirmPassword')?.value ?? '';
    if (!cp) {
      return null;
    }
    return np === cp ? null : { profilePasswordMismatch: true };
  };
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, UxStateComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  /** Example password meeting profile rules (7–12 chars, upper, 2 digits, special). */
  readonly exampleStrongPassword = 'Aa3!x7b';

  isLoading = false;
  isPasswordLoading = false;
  isSessionsLoading = false;
  profileSubmitError: UiLoadError | null = null;
  passwordSubmitError: UiLoadError | null = null;
  sessionsLoadError: UiLoadError | null = null;
  sessions: UserSessionResponse[] = [];
  sessionsHistory: UserSessionResponse[] = [];
  isSessionsHistoryLoading = false;
  sessionsHistoryLoadError: UiLoadError | null = null;
  revokingSessionId: string | null = null;
  revokingOtherSessions = false;
  me: MeResponse | null = null;
  selectedTab: 'personal' | 'security' | 'sessions' | 'access' = 'personal';

  /** Sign-in email from the session; sent on profile save and not editable in the UI. */
  initialEmail = '';

  readonly form = this.formBuilder.nonNullable.group({
    fullName: [''],
    email: [{ value: '', disabled: true }, [Validators.required, Validators.email]]
  });

  readonly passwordForm = this.formBuilder.nonNullable.group(
    {
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, profilePasswordStrengthValidator()]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: [profilePasswordMatchValidator()] }
  );

  constructor(
    private readonly authService: AuthService,
    private readonly criticalActionConfirmationService: CriticalActionConfirmationService,
    private readonly toastr: ToastrService
  ) {}

  private get newPasswordValue(): string {
    return this.passwordForm.controls.newPassword.value ?? '';
  }

  showNewPasswordSuggestions(): boolean {
    return this.newPasswordValue.length > 0 && !this.newPasswordStrongEnough();
  }

  passwordLengthRuleMet(): boolean {
    const v = this.newPasswordValue;
    return v.length >= 7 && v.length <= 12;
  }

  passwordUppercaseMet(): boolean {
    return /[A-Z]/.test(this.newPasswordValue);
  }

  passwordTwoDigitsMet(): boolean {
    return (this.newPasswordValue.match(/\d/g) ?? []).length >= 2;
  }

  passwordSpecialMet(): boolean {
    return hasPasswordSpecialChar(this.newPasswordValue);
  }

  /** All profile password rules satisfied (same bar as “strong”). */
  newPasswordStrongEnough(): boolean {
    return (
      this.passwordLengthRuleMet() &&
      this.passwordUppercaseMet() &&
      this.passwordTwoDigitsMet() &&
      this.passwordSpecialMet()
    );
  }

  /**
   * 0–100: length (up to 25), uppercase (25), two digits (25), special (25).
   * Partial credit for length below 7 and for a single digit.
   */
  newPasswordStrengthPercent(): number {
    const v = this.newPasswordValue;
    if (!v) {
      return 0;
    }
    let score = 0;
    if (v.length >= 7 && v.length <= 12) {
      score += 25;
    } else if (v.length < 7) {
      score += (25 * v.length) / 7;
    }

    if (/[A-Z]/.test(v)) {
      score += 25;
    }
    const digitCount = (v.match(/\d/g) ?? []).length;
    if (digitCount >= 2) {
      score += 25;
    } else if (digitCount === 1) {
      score += 12.5;
    }
    if (hasPasswordSpecialChar(v)) {
      score += 25;
    }
    return Math.min(100, Math.round(score));
  }

  newPasswordStrengthLabel(): string {
    const p = this.newPasswordStrengthPercent();
    if (p === 0) {
      return 'Enter a new password';
    }
    if (p < 40) {
      return 'Weak';
    }
    if (p < 70) {
      return 'Fair';
    }
    if (p < 100) {
      return 'Good';
    }
    return 'Strong';
  }

  newPasswordStrengthBarClass(): string {
    const p = this.newPasswordStrengthPercent();
    if (p === 0) {
      return 'bg-slate-300';
    }
    if (p < 40) {
      return 'bg-rose-500';
    }
    if (p < 70) {
      return 'bg-amber-500';
    }
    if (p < 100) {
      return 'bg-lime-500';
    }
    return 'bg-emerald-600';
  }

  /** Actionable tips when the new password field has text but rules are not all met. */
  newPasswordSuggestions(): string[] {
    const v = this.newPasswordValue;
    if (!v || this.newPasswordStrongEnough()) {
      return [];
    }
    const tips: string[] = [];
    const len = v.length;
    if (len < 7) {
      tips.push(`Use at least 7 characters (you have ${len}); maximum is 12.`);
    } else if (len > 12) {
      tips.push('Shorten the password to 12 characters or fewer.');
    }
    if (!/[A-Z]/.test(v)) {
      tips.push('Add at least one uppercase letter (for example A, M, or Z).');
    }
    const digitCount = (v.match(/\d/g) ?? []).length;
    if (digitCount === 0) {
      tips.push('Include at least two digits (for example 3 and 8).');
    } else if (digitCount === 1) {
      tips.push('Add one more digit so there are at least two numbers in total.');
    }
    if (!hasPasswordSpecialChar(v)) {
      tips.push('Add a special character such as ! @ # $ % ^ & * _ - or ? (not a space).');
    }
    if (tips.length === 0) {
      tips.push('Adjust the password until every rule below is satisfied.');
    }
    return tips;
  }

  ngOnInit(): void {
    const snapshot = this.authService.getProfileSnapshotFromToken();
    if (snapshot) {
      this.form.patchValue({
        fullName: snapshot.fullName,
        email: snapshot.email
      });
      this.initialEmail = snapshot.email.trim().toLowerCase();
    }

    this.authService.getMe().subscribe({
      next: (me) => {
        this.me = me;
        this.form.patchValue(
          {
            fullName: me.fullName ?? '',
            email: me.email ?? ''
          },
          { emitEvent: false }
        );
        this.initialEmail = (me.email ?? '').trim().toLowerCase();
      }
    });

    this.loadSessions();
    this.loadSessionsHistory();
  }

  loadSessions(): void {
    this.isSessionsLoading = true;
    this.sessionsLoadError = null;

    this.authService
      .getSessions()
      .pipe(finalize(() => (this.isSessionsLoading = false)))
      .subscribe({
        next: (sessions) => {
          this.sessions = sessions;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.sessionsLoadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Sessions');
        }
      });
  }

  loadSessionsHistory(): void {
    this.isSessionsHistoryLoading = true;
    this.sessionsHistoryLoadError = null;

    this.authService
      .getSessionsHistory(20)
      .pipe(finalize(() => (this.isSessionsHistoryLoading = false)))
      .subscribe({
        next: (history) => {
          this.sessionsHistory = history;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.sessionsHistoryLoadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Login history');
        }
      });
  }

  revokeSession(session: UserSessionResponse): void {
    if (this.revokingSessionId) {
      return;
    }

    const confirmed = session.isCurrent
      ? this.criticalActionConfirmationService.confirmRevokeCurrentSession()
      : this.criticalActionConfirmationService.confirmRevokeSession();
    if (!confirmed) {
      return;
    }

    this.revokingSessionId = session.id;
    this.sessionsLoadError = null;

    this.authService
      .revokeSession(session.id)
      .pipe(finalize(() => (this.revokingSessionId = null)))
      .subscribe({
        next: () => {
          if (session.isCurrent) {
            this.toastr.success('Session revoked. Please sign in again.', 'Sessions');
            this.authService.clearClientSessionAndNavigateToLogin();
            return;
          }

          this.sessions = this.sessions.filter((entry) => entry.id !== session.id);
          this.sessionsHistory = this.sessionsHistory.map((entry) =>
            entry.id === session.id
              ? {
                  ...entry,
                  isActive: false,
                  revokedAt: new Date().toISOString()
                }
              : entry
          );
          this.toastr.success('Session revoked.', 'Sessions');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.sessionsLoadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Sessions');
        }
      });
  }

  isRevokingSession(sessionId: string): boolean {
    return this.revokingSessionId === sessionId;
  }

  revokeAllOtherSessions(): void {
    if (this.revokingOtherSessions) {
      return;
    }

    const hasOtherSessions = this.sessions.some((session) => !session.isCurrent);
    if (!hasOtherSessions) {
      this.toastr.info('No other active sessions to revoke.', 'Sessions');
      return;
    }

    const confirmed = this.criticalActionConfirmationService.confirmRevokeOtherSessions();
    if (!confirmed) {
      return;
    }

    this.revokingOtherSessions = true;
    this.sessionsLoadError = null;

    this.authService
      .revokeOtherSessions()
      .pipe(finalize(() => (this.revokingOtherSessions = false)))
      .subscribe({
        next: () => {
          this.sessions = this.sessions.filter((session) => session.isCurrent);
          this.loadSessionsHistory();
          this.toastr.success('All other sessions were revoked.', 'Sessions');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.sessionsLoadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Sessions');
        }
      });
  }

  setTab(tab: 'personal' | 'security' | 'sessions' | 'access'): void {
    this.selectedTab = tab;
  }

  tabClass(tab: 'personal' | 'security' | 'sessions' | 'access'): string {
    return this.selectedTab === tab
      ? 'bg-white text-slate-900 shadow-sm dark:bg-slate-900 dark:text-slate-50'
      : 'text-slate-500 hover:text-slate-900 dark:text-slate-300 dark:hover:text-slate-50';
  }

  currentSessionsCount(): number {
    return this.sessions.filter((session) => session.isCurrent).length;
  }

  historyActiveCount(): number {
    return this.sessionsHistory.filter((session) => session.isActive).length;
  }

  historyRevokedCount(): number {
    return this.sessionsHistory.filter((session) => !session.isActive).length;
  }

  sessionStatusClass(session: UserSessionResponse): string {
    if (session.isCurrent) {
      return 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200';
    }

    if (session.isActive) {
      return 'bg-sky-50 text-sky-700 ring-1 ring-sky-200';
    }

    return 'bg-slate-100 text-slate-700 ring-1 ring-slate-200';
  }

  sessionStatusText(session: UserSessionResponse): string {
    if (session.isCurrent) {
      return 'Current';
    }

    return session.isActive ? 'Active' : 'Revoked';
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.initialEmail.trim()) {
      this.toastr.warning('Could not resolve your account email. Please sign in again.', 'Profile');
      return;
    }

    const rawFullName = this.form.controls.fullName.value.trim();

    this.isLoading = true;
    this.profileSubmitError = null;
    this.authService
      .updateProfile({ fullName: rawFullName, email: this.initialEmail })
      .pipe(
        switchMap((profile: ProfileResponse) =>
          this.authService.refreshSession().pipe(
            map(() => profile),
            catchError(() => {
              this.toastr.warning(
                'Profile was saved, but your session could not be refreshed. Please sign in again.',
                'Profile'
              );
              this.authService.clearClientSessionAndNavigateToLogin();
              return EMPTY;
            })
          )
        ),
        finalize(() => (this.isLoading = false))
      )
      .subscribe({
        next: (profile) => {
          this.toastr.success('Profile updated.', 'Profile');
          this.initialEmail = (profile.email ?? '').trim().toLowerCase();
          const snap = this.authService.getProfileSnapshotFromToken();
          if (snap) {
            this.form.patchValue({ fullName: snap.fullName, email: snap.email }, { emitEvent: false });
          }
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.profileSubmitError =
            mapped.kind === 'unknown'
              ? { kind: 'unknown', message: profileUpdateErrorMessage(err) }
              : mapped;
          this.toastr.error(toastMessageForUiLoadError(this.profileSubmitError), 'Profile');
        }
      });
  }

  submitPassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    this.isPasswordLoading = true;
    this.authService
      .changePassword({
        currentPassword: this.passwordForm.controls.currentPassword.value,
        newPassword: this.passwordForm.controls.newPassword.value
      })
      .pipe(finalize(() => (this.isPasswordLoading = false)))
      .subscribe({
        next: () => {
          this.toastr.success('Password changed. Please sign in again.', 'Password Change');
          this.authService.clearClientSessionAndNavigateToLogin();
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.passwordSubmitError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Password Change');
        }
      });
  }
}
