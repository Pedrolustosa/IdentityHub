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
import { HttpErrorResponse } from '@angular/common/http';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { finalize, switchMap } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import { PhoneInputComponent } from '../../../../shared/components/phone-input/phone-input.component';
import {
  normalizeOptionalDate,
  normalizeOptionalText,
  optionalDateOfBirthValidator
} from '../../../../shared/validation/user-contact.validators';

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
  imports: [CommonModule, ReactiveFormsModule, UxStateComponent, PhoneInputComponent],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  /** Example password meeting profile rules (7–12 chars, upper, 2 digits, special). */
  readonly exampleStrongPassword = 'Aa3!x7b';

  isLoading = false;
  isPasswordLoading = false;
  profileSubmitError: UiLoadError | null = null;
  passwordSubmitError: UiLoadError | null = null;

  /** Sign-in email from the session; sent on profile save and not editable in the UI. */
  initialEmail = '';

  readonly form = this.formBuilder.nonNullable.group({
    fullName: [''],
    email: [{ value: '', disabled: true }, [Validators.required, Validators.email]],
    phoneNumber: [''],
    dateOfBirth: ['', [optionalDateOfBirthValidator()]]
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

  newPasswordStrongEnough(): boolean {
    return (
      this.passwordLengthRuleMet() &&
      this.passwordUppercaseMet() &&
      this.passwordTwoDigitsMet() &&
      this.passwordSpecialMet()
    );
  }

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
      return 'bg-panel-muted';
    }
    if (p < 40) {
      return 'bg-danger-500';
    }
    if (p < 70) {
      return 'bg-warning-500';
    }
    if (p < 100) {
      return 'bg-success-400';
    }
    return 'bg-success-600';
  }

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
        this.form.patchValue(
          {
            fullName: me.fullName ?? '',
            email: me.email ?? '',
            phoneNumber: me.phoneNumber ?? '',
            dateOfBirth: me.dateOfBirth ?? ''
          },
          { emitEvent: false }
        );
        this.initialEmail = (me.email ?? '').trim().toLowerCase();
      }
    });
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
    const phoneNumber = normalizeOptionalText(this.form.controls.phoneNumber.value);
    const dateOfBirth = normalizeOptionalDate(this.form.controls.dateOfBirth.value);

    this.isLoading = true;
    this.profileSubmitError = null;
    this.authService
      .updateProfile({
        fullName: rawFullName,
        email: this.initialEmail,
        phoneNumber,
        dateOfBirth
      })
      .pipe(
        switchMap(() => this.authService.getMe()),
        finalize(() => (this.isLoading = false))
      )
      .subscribe({
        next: (me) => {
          this.toastr.success('Profile updated.', 'Profile');
          this.initialEmail = (me.email ?? '').trim().toLowerCase();
          this.form.patchValue(
            {
              fullName: me.fullName ?? '',
              email: me.email ?? '',
              phoneNumber: me.phoneNumber ?? '',
              dateOfBirth: me.dateOfBirth ?? ''
            },
            { emitEvent: false }
          );
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
