import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { EMPTY, catchError, finalize, map, switchMap } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService, ProfileResponse } from '../../services/auth.service';

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

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})
export class ProfileComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  isLoading = false;
  isPasswordLoading = false;

  private initialEmail = '';

  readonly form = this.formBuilder.nonNullable.group({
    fullName: [''],
    email: ['', [Validators.required, Validators.email]]
  });

  readonly passwordForm = this.formBuilder.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  });

  constructor(
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    const snapshot = this.authService.getProfileSnapshotFromToken();
    if (snapshot) {
      this.form.patchValue({
        fullName: snapshot.fullName,
        email: snapshot.email
      });
      this.initialEmail = snapshot.email.trim().toLowerCase();
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const rawEmail = this.form.controls.email.value.trim();
    const rawFullName = this.form.controls.fullName.value.trim();
    const emailBefore = this.initialEmail;

    this.isLoading = true;
    this.authService
      .updateProfile({ fullName: rawFullName, email: rawEmail })
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
          const newEmail = (profile.email ?? '').trim().toLowerCase();
          const emailChanged = Boolean(emailBefore && newEmail && emailBefore !== newEmail);
          const successMsg = emailChanged
            ? 'Profile updated. Confirm your new email when prompted.'
            : 'Profile updated.';
          this.toastr.success(successMsg, 'Profile');
          this.initialEmail = newEmail;
          const snap = this.authService.getProfileSnapshotFromToken();
          if (snap) {
            this.form.patchValue({ fullName: snap.fullName, email: snap.email }, { emitEvent: false });
          }
        },
        error: (err: unknown) => {
          this.toastr.error(profileUpdateErrorMessage(err), 'Profile');
        }
      });
  }

  passwordsMatch(): boolean {
    return (
      this.passwordForm.controls.newPassword.value ===
      this.passwordForm.controls.confirmPassword.value
    );
  }

  submitPassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    if (!this.passwordsMatch()) {
      this.toastr.error('New password and confirmation do not match.', 'Password');
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
          this.toastr.success('Password changed. Please sign in again.', 'Password');
          this.authService.clearClientSessionAndNavigateToLogin();
        },
        error: () => {
          this.toastr.error('Could not change password. Check your current password.', 'Password');
        }
      });
  }
}
